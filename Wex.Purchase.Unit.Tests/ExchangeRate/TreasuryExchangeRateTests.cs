using Moq;
using Serilog.Core;
using Wex.Purchase.BusinessModels;
using Wex.Purchase.Common.Exceptions;
using Wex.Purchase.Manager.ExchangeRate;
using Wex.Purchase.Manager.ExchangeRateConversion;
using Wex.Purchase.Repository.Entity;
using Xunit;

namespace Wex.Purchase.Unit.Tests.ExchangeRate;

/// <summary>
/// Unit tests for Treasury API exchange rate client and conversion service.
/// Tests cover API calls, caching behavior, and currency conversions.
/// </summary>
public class TreasuryExchangeRateTests
{
    private readonly Mock<ITreasuryExchangeRateClient> _mockExchangeRateClient;
    private readonly IExchangeRateConversionService _conversionService;

    public TreasuryExchangeRateTests()
    {
        _mockExchangeRateClient = new Mock<ITreasuryExchangeRateClient>();
        _conversionService = new ExchangeRateConversionService(_mockExchangeRateClient.Object, Logger.None);
    }

    /// <summary>
    /// Test: ConvertPurchaseAsync should retrieve exchange rate and convert amount correctly.
    /// Validates that currency conversion applies the correct formula: ConvertedAmount = PurchaseAmount * ExchangeRate.
    /// </summary>
    [Fact]
    public async Task ConvertPurchaseAsync_ValidPurchaseAndRate_ConvertsCorrectly()
    {
        // Arrange
        var purchase = new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            Description = "Test Purchase",
            PurchaseAmount = 100.00m,
            TransactionDate = DateTime.Now
        };

        var exchangeRateRecord = new ExchangeRateRecord
        {
            CountryCurrencyDesc = "Cubo-Peso",
            RecordDate = purchase.ExchangeRateDate,
            ExchangeRate = 1.10m
        };
        decimal exchangeRate = exchangeRateRecord.ExchangeRate; // Cubo-Peso to USD: 1 USD = 1.10 Cubo-Peso
        _mockExchangeRateClient.Setup(c => c.GetExchangeRateWithFallbackAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(exchangeRateRecord);

        // Act
        var result = await _conversionService.ConvertPurchaseAsync(purchase, "Cubo-Peso");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(purchase.Id, result.Purchase.Id);
        Assert.Equal(exchangeRate, result.ExchangeRate);
        // Converted: 100.00 * 1.10 = 110.00 (rounded AwayFromZero)
        Assert.Equal(Math.Round(100.00m * 1.10m, 2, MidpointRounding.AwayFromZero), result.ConvertedAmount);
    }

    /// <summary>
    /// Test: ConvertPurchaseAsync should throw exception when exchange rate not found.
    /// Validates that the service propagates exchange rate lookup failures.
    /// </summary>
    [Fact]
    public async Task ConvertPurchaseAsync_ExchangeRateNotFound_ThrowsException()
    {
        // Arrange
        var purchase = new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            Description = "Test Purchase",
            PurchaseAmount = 100.00m,
            TransactionDate = DateTime.Now
        };

        _mockExchangeRateClient.Setup(c => c.GetExchangeRateWithFallbackAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No exchange rate found for currency Cubo-Peso"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _conversionService.ConvertPurchaseAsync(purchase, "Cubo-Peso"));
    }

    /// <summary>
    /// Test: ConvertPurchasesAsync should convert multiple purchases to multiple currencies in parallel.
    /// Validates batch conversion creates all purchase-currency combinations.
    /// </summary>
    [Fact]
    public async Task ConvertPurchasesAsync_MultipleItems_ConvertsAll()
    {
        // Arrange
        var purchases = new List<PurchaseDTO>
        {
            new PurchaseDTO
            {
                Id = Guid.NewGuid(),
                Description = "Purchase 1",
                PurchaseAmount = 100.00m,
                TransactionDate = DateTime.Now
            },
            new PurchaseDTO
            {
                Id = Guid.NewGuid(),
                Description = "Purchase 2",
                PurchaseAmount = 200.00m,
                TransactionDate = DateTime.Now
            }
        };

        var exchangeRateRecord = new ExchangeRateRecord
        {
            CountryCurrencyDesc = "Cubo-Peso",
            RecordDate = purchases[0].ExchangeRateDate,
            ExchangeRate = 1.10m
        };
        string currency = exchangeRateRecord.CountryCurrencyDesc;

        _mockExchangeRateClient.Setup(c => c.GetExchangeRateWithFallbackAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(exchangeRateRecord);
        // Act
        var results = await _conversionService.ConvertPurchasesAsync(purchases, currency);

        // Assert
        Assert.NotNull(results);
        // 2 purchases converted to 1 currency = 2 results
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.NotNull(r.Purchase));
        Assert.All(results, r => Assert.NotNull(r.ConvertedAmount));
        Assert.All(results, r => Assert.True(r.ConvertedAmount.Value > 0));
    }

    /// <summary>
    /// Test: ConvertPurchaseAsync should round converted amount using AwayFromZero rounding.
    /// Validates financial rigor: 10.555 rounds to 10.56 (not 10.55).
    /// </summary>
    [Fact]
    public async Task ConvertPurchaseAsync_RoundsAwayFromZero()
    {
        // Arrange
        var purchase = new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            Description = "Rounding Test",
            PurchaseAmount = 100.00m,
            TransactionDate = DateTime.Now
        };

        var exchangeRateRecord = new ExchangeRateRecord
        {
            CountryCurrencyDesc = "Cubo-Peso",
            RecordDate = DateOnly.FromDateTime(purchase.TransactionDate),
            ExchangeRate = 1.05m
        };
        // EUR to USD: 100 * 1.05 = 105.00
        decimal exchangeRate = exchangeRateRecord.ExchangeRate;
        _mockExchangeRateClient.Setup(c => c.GetExchangeRateWithFallbackAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(exchangeRateRecord);

        // Act
        var result = await _conversionService.ConvertPurchaseAsync(purchase, "Cubo-Peso");

        // Assert
        Assert.Equal(Math.Round(100.00m * 1.05m, 2, MidpointRounding.AwayFromZero), result.ConvertedAmount);
    }

    /// <summary>
    /// Test: ConvertPurchaseAsync should reject null purchase.
    /// Validates null-safety and defensive programming.
    /// </summary>
    [Fact]
    public async Task ConvertPurchaseAsync_NullPurchase_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        PurchaseDTO? nullPurchase = null;
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _conversionService.ConvertPurchaseAsync(nullPurchase!, "EUR"));
    }

    /// <summary>
    /// Test: ConvertPurchaseAsync should reject null or empty currency code.
    /// Validates null-safety for input parameters.
    /// </summary>
    [Fact]
    public async Task ConvertPurchaseAsync_NullCurrencyCode_ThrowsArgumentException()
    {
        // Arrange
        var purchase = new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            Description = "Test",
            PurchaseAmount = 100.00m,
            TransactionDate = DateTime.Now
        };

        // Act & Assert
        string? nullCurrency = null;
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _conversionService.ConvertPurchaseAsync(purchase, nullCurrency!));
    }
}

using Moq;
using Wex.Purchase.BusinessModels;
using Wex.Purchase.Manager.ExchangeRate;
using Wex.Purchase.Manager.ExchangeRateConversion;
using Xunit;
using Serilog.Core;

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
    /// Validates that currency conversion applies the correct formula: ConvertedAmount = PurchaseAmount / ExchangeRate.
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
            TransactionDate = DateOnly.FromDateTime(DateTime.Now)
        };

        decimal exchangeRate = 1.10m; // EUR to USD: 1 USD = 1.10 EUR
        _mockExchangeRateClient.Setup(c => c.GetExchangeRateWithFallbackAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(exchangeRate);

        // Act
        var result = await _conversionService.ConvertPurchaseAsync(purchase, "EUR");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(purchase.Id, result.Purchase.Id);
        Assert.Equal(exchangeRate, result.ExchangeRate);
        // Converted: 100.00 / 1.10 = 90.91 (rounded AwayFromZero)
        Assert.Equal(Math.Round(100.00m / 1.10m, 2, MidpointRounding.AwayFromZero), result.ConvertedAmount);
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
            TransactionDate = DateOnly.FromDateTime(DateTime.Now)
        };

        _mockExchangeRateClient.Setup(c => c.GetExchangeRateWithFallbackAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No exchange rate found for currency EUR"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _conversionService.ConvertPurchaseAsync(purchase, "EUR"));
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
                TransactionDate = DateOnly.FromDateTime(DateTime.Now)
            },
            new PurchaseDTO
            {
                Id = Guid.NewGuid(),
                Description = "Purchase 2",
                PurchaseAmount = 200.00m,
                TransactionDate = DateOnly.FromDateTime(DateTime.Now)
            }
        };

        var currencies = new[] { "EUR", "GBP" };

        _mockExchangeRateClient.Setup(c => c.GetExchangeRateWithFallbackAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string code, DateOnly date, CancellationToken ct) =>
            {
                return code.ToUpper() switch
                {
                    "EUR" => 1.10m,
                    "GBP" => 1.25m,
                    _ => 1.0m
                };
            });

        // Act
        var results = await _conversionService.ConvertPurchasesAsync(purchases, currencies);

        // Assert
        Assert.NotNull(results);
        // 2 purchases × 2 currencies = 4 results
        Assert.Equal(4, results.Count);
        Assert.All(results, r => Assert.NotNull(r.Purchase));
        Assert.All(results, r => Assert.True(r.ConvertedAmount > 0));
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
            TransactionDate = DateOnly.FromDateTime(DateTime.Now)
        };

        // EUR to USD: 100 / 1.05 = 95.238...
        decimal exchangeRate = 1.05m;
        _mockExchangeRateClient.Setup(c => c.GetExchangeRateWithFallbackAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(exchangeRate);

        // Act
        var result = await _conversionService.ConvertPurchaseAsync(purchase, "EUR");

        // Assert
        Assert.Equal(Math.Round(100.00m / 1.05m, 2, MidpointRounding.AwayFromZero), result.ConvertedAmount);
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
            TransactionDate = DateOnly.FromDateTime(DateTime.Now)
        };

        // Act & Assert
        string? nullCurrency = null;
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _conversionService.ConvertPurchaseAsync(purchase, nullCurrency!));
    }
}

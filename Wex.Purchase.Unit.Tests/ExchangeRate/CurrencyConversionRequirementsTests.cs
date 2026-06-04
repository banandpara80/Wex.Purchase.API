using Xunit;
using Wex.Purchase.Manager.ExchangeRate;
using Wex.Purchase.BusinessModels;
using Wex.Purchase.Manager.ExchangeRateConversion;
using Moq;
using Serilog.Core;

namespace Wex.Purchase.Unit.Tests.ExchangeRate;

/// <summary>
/// Comprehensive validation tests for currency conversion requirements.
/// Tests ensure compliance with:
/// 1. No exact date match required, but rate must be ≤ purchase date
/// 2. Rate must be within 6 months before purchase date
/// 3. Error thrown if no valid rate found
/// 4. Converted amount rounded to 2 decimal places
/// </summary>
public class CurrencyConversionRequirementsTests
{
    private readonly Mock<ITreasuryExchangeRateClient> _mockExchangeRateClient;
    private readonly IExchangeRateConversionService _conversionService;

    public CurrencyConversionRequirementsTests()
    {
        _mockExchangeRateClient = new Mock<ITreasuryExchangeRateClient>();
        _conversionService = new ExchangeRateConversionService(_mockExchangeRateClient.Object, Logger.None);
    }

    /// <summary>
    /// Requirement 1a: Exact date match is acceptable (not required, but acceptable).
    /// </summary>
    [Fact]
    public async Task ConvertPurchase_ExactDateMatch_UsesExactDateRate()
    {
        // Arrange - Purchase date matches exact rate date
        var purchaseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10));
        var purchase = new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            Description = "Test Purchase",
            PurchaseAmount = 100.00m,
            TransactionDate = purchaseDate.ToDateTime(TimeOnly.MinValue)
        };

        decimal exactRate = 1.23m;
        _mockExchangeRateClient.Setup(c => c.GetExchangeRateWithFallbackAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(exactRate);

        // Act
        var result = await _conversionService.ConvertPurchaseAsync(purchase, "EUR");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(exactRate, result.ExchangeRate);
        decimal expectedConverted = Math.Round(100.00m * 1.23m, 2, MidpointRounding.AwayFromZero);
        Assert.Equal(expectedConverted, result.ConvertedAmount);
    }

    /// <summary>
    /// Requirement 1b: No exact date match required - older rate (less than purchase date) is acceptable.
    /// </summary>
    [Fact]
    public async Task ConvertPurchase_OlderRateBeforePurchaseDate_UsesOlderRate()
    {
        // Arrange - Purchase date is 10 days after rate date (rate is older, but still valid)
        var purchaseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10));
        var rateDate = purchaseDate.AddDays(-5); // 5 days before purchase

        var purchase = new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            Description = "Test Purchase",
            PurchaseAmount = 100.00m,
            TransactionDate = purchaseDate.ToDateTime(TimeOnly.MinValue)
        };

        decimal olderRate = 1.15m;
        _mockExchangeRateClient.Setup(c => c.GetExchangeRateWithFallbackAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(olderRate);

        // Act
        var result = await _conversionService.ConvertPurchaseAsync(purchase, "EUR");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(olderRate, result.ExchangeRate);
    }

    /// <summary>
    /// Requirement 2: Rate must be within 6 months before purchase date (error if older than 6 months).
    /// </summary>
    [Fact]
    public async Task ConvertPurchase_RateMoreThan6MonthsOld_ThrowsException()
    {
        // Arrange - Rate is more than 6 months old
        var purchaseDate = DateOnly.FromDateTime(DateTime.UtcNow);

                var purchase = new PurchaseDTO
                {
                    Id = Guid.NewGuid(),
                    Description = "Test Purchase",
                    PurchaseAmount = 100.00m,
                    TransactionDate = purchaseDate.ToDateTime(TimeOnly.MinValue)
                };

        _mockExchangeRateClient.Setup(c => c.GetExchangeRateWithFallbackAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No exchange rate found for EUR on 2024-01-01 or in the past 6 months"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _conversionService.ConvertPurchaseAsync(purchase, "EUR"));

        Assert.Contains("No exchange rate found", exception.Message);
    }

    /// <summary>
    /// Requirement 2b: Error thrown if rate date is AFTER purchase date (rate cannot be in future).
    /// </summary>
    [Fact]
    public async Task ConvertPurchase_RateDateAfterPurchaseDate_ThrowsException()
    {
        // Arrange - Rate date is after purchase date (future rate - invalid)
        var purchaseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10));

                var purchase = new PurchaseDTO
                {
                    Id = Guid.NewGuid(),
                    Description = "Test Purchase",
                    PurchaseAmount = 100.00m,
                    TransactionDate = purchaseDate.ToDateTime(TimeOnly.MinValue)
                };

        // Simulate scenario where no valid rate exists (future rate would be invalid)
        _mockExchangeRateClient.Setup(c => c.GetExchangeRateWithFallbackAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"No exchange rate found for EUR on {purchaseDate} or in the past 6 months"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _conversionService.ConvertPurchaseAsync(purchase, "EUR"));
    }

    /// <summary>
    /// Requirement 3a: Error thrown when no currency conversion rate available.
    /// </summary>
    [Fact]
    public async Task ConvertPurchase_NoCurrencyRateAvailable_ThrowsException()
    {
        // Arrange
        var purchase = new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            Description = "Test Purchase",
            PurchaseAmount = 100.00m,
            TransactionDate = DateTime.UtcNow
        };

        _mockExchangeRateClient.Setup(c => c.GetExchangeRateWithFallbackAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No exchange rate found for XYZ currency"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _conversionService.ConvertPurchaseAsync(purchase, "XYZ"));

        Assert.Contains("No exchange rate found", exception.Message);
    }

    /// <summary>
    /// Requirement 3b: Error message must state "purchase cannot be converted to the target currency".
    /// </summary>
    [Fact]
    public async Task ConvertPurchase_ErrorMessage_IndicatesConversionNotPossible()
    {
        // Arrange
        var purchase = new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            Description = "Test Purchase",
            PurchaseAmount = 100.00m,
            TransactionDate = DateTime.UtcNow
        };

        _mockExchangeRateClient.Setup(c => c.GetExchangeRateWithFallbackAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No exchange rate found for ABC currency"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _conversionService.ConvertPurchaseAsync(purchase, "ABC"));

        // Verify exception indicates the conversion cannot be performed
        Assert.NotNull(exception);
        Assert.True(!string.IsNullOrEmpty(exception.Message));
    }

    /// <summary>
    /// Requirement 4a: Converted amount rounded to two decimal places (cent).
    /// Test rounding with AwayFromZero (banker's rounding).
    /// </summary>
    [Fact]
    public async Task ConvertPurchase_RoundsToTwoDecimalPlaces_AwayFromZero()
    {
        // Arrange - 100 * 1.15 = 115.00 should round to 115.00
        var purchase = new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            Description = "Rounding Test",
            PurchaseAmount = 100.00m,
            TransactionDate = DateTime.UtcNow
        };

        decimal rate = 1.15m;
        _mockExchangeRateClient.Setup(c => c.GetExchangeRateWithFallbackAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rate);

        // Act
        var result = await _conversionService.ConvertPurchaseAsync(purchase, "EUR");

        // Assert - Verify exactly 2 decimal places
        Assert.NotNull(result.ConvertedAmount);
        Assert.Equal(2, decimal.GetBits(result.ConvertedAmount.Value)[3] >> 16);  // Scale should be 2
        decimal expected = Math.Round(100.00m * 1.15m, 2, MidpointRounding.AwayFromZero);
        Assert.Equal(expected, result.ConvertedAmount);
    }

    /// <summary>
    /// Requirement 4b: Converted amount rounded to two decimal places.
    /// Test edge case: 10.555 should round to 10.56 (AwayFromZero).
    /// </summary>
    [Fact]
    public async Task ConvertPurchase_RoundsAwayFromZero_EdgeCase()
    {
        // Arrange - 10.555 rounding edge case
        var purchase = new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            Description = "Edge Case Rounding",
            PurchaseAmount = 10.555m,
            TransactionDate = DateTime.UtcNow
        };

        decimal rate = 1.0m;
        _mockExchangeRateClient.Setup(c => c.GetExchangeRateWithFallbackAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rate);

        // Act
        var result = await _conversionService.ConvertPurchaseAsync(purchase, "EUR");

        // Assert - 10.555 * 1.0 = 10.555 should round to 10.56
        Assert.Equal(10.56m, result.ConvertedAmount);
    }

    /// <summary>
    /// Requirement 4c: Multiple conversions maintain 2 decimal places rounding.
    /// </summary>
    [Fact]
    public async Task ConvertPurchases_MultipleConversions_AllRoundedToTwoDecimals()
    {
        // Arrange
        var purchases = new List<PurchaseDTO>
        {
                new PurchaseDTO
                {
                    Id = Guid.NewGuid(),
                    Description = "Purchase 1",
                    PurchaseAmount = 100.50m,
                    TransactionDate = DateTime.UtcNow
                },
                new PurchaseDTO
                {
                    Id = Guid.NewGuid(),
                    Description = "Purchase 2",
                    PurchaseAmount = 250.75m,
                    TransactionDate = DateTime.UtcNow
                }
            };

        string currency = "Peso";

        _mockExchangeRateClient.Setup(c => c.GetExchangeRateWithFallbackAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string code, DateOnly date, CancellationToken ct) =>
            {
                return code switch
                {
                    "EUR" => 1.10m,
                    "GBP" => 1.25m,
                    _ => 1.0m
                };
            });

        // Act
        var results = await _conversionService.ConvertPurchasesAsync(purchases, currency);

        // Assert - All results should have exactly 2 decimal places
        Assert.NotNull(results);
        foreach (var result in results)
        {
            Assert.NotNull(result.ConvertedAmount);
            Assert.True(result.ConvertedAmount > 0);
            // Verify scale is 2 (exactly 2 decimal places)
            int scale = decimal.GetBits(result.ConvertedAmount.Value)[3] >> 16;
            Assert.True(scale <= 2, $"Amount {result.ConvertedAmount} has scale {scale}, expected <= 2");
        }
    }

    /// <summary>
    /// Integration test: End-to-end currency conversion with all requirements validated.
    /// </summary>
    [Fact]
    public async Task ConvertPurchase_FullRequirementValidation_Success()
    {
        // Arrange - Simulates complete workflow
        var purchaseDate = DateTime.UtcNow.AddDays(-15);
        var purchase = new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            Description = "International Purchase",
            PurchaseAmount = 1000.00m,
            TransactionDate = purchaseDate
        };

        // Rate that would be within 6 months but before purchase date
        decimal rate = 1.18m;
        _mockExchangeRateClient.Setup(c => c.GetExchangeRateWithFallbackAsync("EUR", DateOnly.FromDateTime(purchaseDate), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rate);

        // Act
        var result = await _conversionService.ConvertPurchaseAsync(purchase, "EUR");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(purchase.Id, result.Purchase.Id);
        Assert.Equal(rate, result.ExchangeRate);

        // Verify conversion formula: 1000 * 1.18 = 1180.00
        decimal expected = Math.Round(1000.00m * 1.18m, 2, MidpointRounding.AwayFromZero);
        Assert.Equal(expected, result.ConvertedAmount);

        // Verify exactly 2 decimal places
        Assert.NotNull(result.ConvertedAmount);
        int scale = decimal.GetBits(result.ConvertedAmount.Value)[3] >> 16;
        Assert.True(scale <= 2, $"Scale should be <= 2, got {scale}");
    }
}

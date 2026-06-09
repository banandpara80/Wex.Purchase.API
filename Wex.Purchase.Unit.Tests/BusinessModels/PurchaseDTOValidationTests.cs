using System.ComponentModel.DataAnnotations;
using Wex.Purchase.BusinessModels;
using Xunit;

namespace Wex.Purchase.Unit.Tests.BusinessModels;

/// <summary>
/// Unit tests for PurchaseDTO validation, including the new NotFutureDate constraint.
/// </summary>
public class PurchaseDTOValidationTests
{
    /// <summary>
    /// Test: Valid purchase with today's date should pass validation.
    /// </summary>
    [Fact]
    public void PurchaseDTO_WithTodaysDate_IsValid()
    {
        // Arrange
        var dto = new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            Description = "Valid Purchase",
            TransactionDate = DateTime.Now,
            PurchaseAmount = 100.00m
        };

        // Act
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, context, results, validateAllProperties: true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    /// <summary>
    /// Test: Valid purchase with today's date (DateOnly portion) should pass validation.
    /// </summary>
    [Fact]
    public void PurchaseDTO_WithTodaysDateOnly_IsValid()
    {
        // Arrange
        var dto = new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            Description = "Valid Purchase",
            TransactionDate = DateTime.Now.Date.AddHours(12), // Today at noon
            PurchaseAmount = 50.50m
        };

        // Act
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, context, results, validateAllProperties: true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    /// <summary>
    /// Test: Valid purchase with a past date should pass validation.
    /// </summary>
    [Fact]
    public void PurchaseDTO_WithPastDate_IsValid()
    {
        // Arrange
        var pastDate = DateTime.Now.AddDays(-10);
        var dto = new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            Description = "Past Purchase",
            TransactionDate = pastDate,
            PurchaseAmount = 75.25m
        };

        // Act
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, context, results, validateAllProperties: true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    /// <summary>
    /// Test: Purchase with tomorrow's date should fail validation.
    /// </summary>
    [Fact]
    public void PurchaseDTO_WithFutureDate_IsInvalid()
    {
        // Arrange
        var futureDate = DateTime.Now.AddDays(1);
        var dto = new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            Description = "Future Purchase",
            TransactionDate = futureDate,
            PurchaseAmount = 100.00m
        };

        // Act
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        Assert.Single(results);
        Assert.Contains("cannot be in the future", results[0].ErrorMessage ?? string.Empty);
    }

    /// <summary>
    /// Test: Purchase with a date far in the future should fail validation.
    /// </summary>
    [Fact]
    public void PurchaseDTO_WithDistantFutureDate_IsInvalid()
    {
        // Arrange
        var distantFutureDate = DateTime.Now.AddYears(1);
        var dto = new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            Description = "Year Ahead Purchase",
            TransactionDate = distantFutureDate,
            PurchaseAmount = 500.00m
        };

        // Act
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        Assert.Single(results);
        Assert.Contains("cannot be in the future", results[0].ErrorMessage ?? string.Empty);
    }

    /// <summary>
    /// Test: Purchase with default date (DateTime.MinValue) should fail validation.
    /// </summary>
    [Fact]
    public void PurchaseDTO_WithDefaultDate_IsInvalid()
    {
        // Arrange
        var dto = new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            Description = "Default Date Purchase",
            TransactionDate = default,
            PurchaseAmount = 100.00m
        };

        // Act
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        var transactionDateErrors = results.Where(r => r.MemberNames.Contains("TransactionDate")).ToList();
        Assert.NotEmpty(transactionDateErrors);
    }

    /// <summary>
    /// Test: Purchase with missing description should fail validation.
    /// </summary>
    [Fact]
    public void PurchaseDTO_WithMissingDescription_IsInvalid()
    {
        // Arrange
        var dto = new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            Description = string.Empty,
            TransactionDate = DateTime.Now,
            PurchaseAmount = 100.00m
        };

        // Act
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        var descriptionErrors = results.Where(r => r.MemberNames.Contains("Description")).ToList();
        Assert.NotEmpty(descriptionErrors);
    }

    /// <summary>
    /// Test: Purchase with zero amount should fail validation.
    /// </summary>
    [Fact]
    public void PurchaseDTO_WithZeroAmount_IsInvalid()
    {
        // Arrange
        var dto = new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            Description = "Zero Amount Purchase",
            TransactionDate = DateTime.Now,
            PurchaseAmount = 0.00m
        };

        // Act
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        var amountErrors = results.Where(r => r.MemberNames.Contains("PurchaseAmount")).ToList();
        Assert.NotEmpty(amountErrors);
    }

    /// <summary>
    /// Test: Purchase with boundary date (yesterday) should pass validation.
    /// </summary>
    [Fact]
    public void PurchaseDTO_WithYesterdaysDate_IsValid()
    {
        // Arrange
        var yesterdayDate = DateTime.Now.Date.AddDays(-1);
        var dto = new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            Description = "Yesterday Purchase",
            TransactionDate = yesterdayDate,
            PurchaseAmount = 123.45m
        };

        // Act
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, context, results, validateAllProperties: true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    /// <summary>
    /// Test: Purchase with a time in the future but on today's date should fail validation
    /// when time is considered, but pass when only date is compared.
    /// </summary>
    [Fact]
    public void PurchaseDTO_WithFutureTimeToday_IsValid()
    {
        // Arrange
        // Future time today (e.g., current time + 5 hours)
        var futureTimeToday = DateTime.Now.AddHours(5);
        var dto = new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            Description = "Future Time Today",
            TransactionDate = futureTimeToday,
            PurchaseAmount = 50.00m
        };

        // Act
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, context, results, validateAllProperties: true);

        // Assert
        // Should be valid because we compare dates only, not times
        Assert.True(isValid);
        Assert.Empty(results);
    }
}

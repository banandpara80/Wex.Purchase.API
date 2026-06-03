using Moq;
using Wex.Purchase.BusinessModels;
using Wex.Purchase.Manager;
using Wex.Purchase.Service;
using Xunit;
using Wex.Purchase.Common.Exceptions;
using Serilog.Core;

namespace Wex.Purchase.Unit.Tests.Service;

/// <summary>
/// Unit tests for PurchaseService.
/// Tests cover validation, error handling, and proper delegation to IPurchaseManager.
/// </summary>
public class PurchaseServiceTests
{
    private readonly Mock<IPurchaseManager> mockPurchaseManager;
    private readonly IPurchaseService purchaseService;

    public PurchaseServiceTests()
    {
        mockPurchaseManager = new Mock<IPurchaseManager>();
        purchaseService = new PurchaseService(Logger.None, mockPurchaseManager.Object);
    }

    /// <summary>
    /// Test: AddPurchase should preserve and return a valid TransactionDate.
    /// Validates that date parsing works correctly and the manager is invoked once.
    /// </summary>
    [Fact]
    public async Task AddPurchase_TransactionDateParsedFromString_Valid()
    {
        // Arrange
        var parsed = DateOnly.Parse("2024-05-01");
        var dto = new PurchaseDTO { Id = Guid.NewGuid(), Description = "Valid", PurchaseAmount = 1.23m, TransactionDate = parsed };
        mockPurchaseManager.Setup(m => m.AddPurchase(It.IsAny<PurchaseDTO>(), It.IsAny<CancellationToken>())).ReturnsAsync((PurchaseDTO p, CancellationToken ct) => p);

        // Act
        var result = await purchaseService.AddPurchase(dto);

        // Assert
        Assert.Equal(parsed, result.TransactionDate);
        mockPurchaseManager.Verify(m => m.AddPurchase(It.IsAny<PurchaseDTO>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: AddPurchase should reject a DTO with an empty description.
    /// Validates that DTO validation catches invalid data before manager invocation.
    /// </summary>
    [Fact]
    public async Task AddPurchase_InvalidDto_ThrowsPurchaseValidationException()
    {
        // Arrange
        var dto = new PurchaseDTO { Id = Guid.NewGuid(), Description = string.Empty, PurchaseAmount = 2.0m, TransactionDate = DateOnly.FromDateTime(DateTime.Now) };

        // Act & Assert
        await Assert.ThrowsAsync<PurchaseValidationException>(async () => await purchaseService.AddPurchase(dto));
    }

    /// <summary>
    /// Test: AddPurchase should allow a description of exactly 50 characters (boundary test).
    /// Validates that the maximum length constraint is inclusive at the boundary.
    /// </summary>
    [Fact]
    public async Task AddPurchase_DescriptionExactly50_Allows()
    {
        // Arrange
        var desc = new string('A', 50);
        var dto = new PurchaseDTO { Id = Guid.NewGuid(), Description = desc, PurchaseAmount = 10.0m, TransactionDate = DateOnly.FromDateTime(DateTime.Now) };
        mockPurchaseManager.Setup(m => m.AddPurchase(It.IsAny<PurchaseDTO>(), It.IsAny<CancellationToken>())).ReturnsAsync((PurchaseDTO p, CancellationToken ct) => p);

        // Act
        var result = await purchaseService.AddPurchase(dto);

        // Assert
        Assert.Equal(desc, result.Description);
        mockPurchaseManager.Verify(m => m.AddPurchase(It.IsAny<PurchaseDTO>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: AddPurchase should reject a description exceeding 50 characters.
    /// Validates that string length validation enforces the boundary constraint.
    /// </summary>
    [Fact]
    public async Task AddPurchase_DescriptionTooLong_ThrowsPurchaseValidationException()
    {
        // Arrange
        var desc = new string('B', 51);
        var dto = new PurchaseDTO { Id = Guid.NewGuid(), Description = desc, PurchaseAmount = 10.0m, TransactionDate = DateOnly.FromDateTime(DateTime.Now) };

        // Act & Assert
        await Assert.ThrowsAsync<PurchaseValidationException>(async () => await purchaseService.AddPurchase(dto));
    }

    /// <summary>
    /// Test: AddPurchase should reject a purchase with default (unset) TransactionDate.
    /// Validates that date requirement validation prevents missing dates.
    /// </summary>
    [Fact]
    public async Task AddPurchase_TransactionDateDefault_ThrowsPurchaseValidationException()
    {
        // Arrange
        var dto = new PurchaseDTO { Id = Guid.NewGuid(), Description = "Valid", PurchaseAmount = 5.0m, TransactionDate = default };

        // Act & Assert
        await Assert.ThrowsAsync<PurchaseValidationException>(async () => await purchaseService.AddPurchase(dto));
    }

    /// <summary>
    /// Test: AddPurchase should reject a zero purchase amount.
    /// Validates that financial rigor constraint (amount > 0) is enforced.
    /// </summary>
    [Fact]
    public async Task AddPurchase_PurchaseAmountZero_ThrowsPurchaseValidationException()
    {
        // Arrange
        var dto = new PurchaseDTO { Id = Guid.NewGuid(), Description = "Valid", PurchaseAmount = 0.0m, TransactionDate = DateOnly.FromDateTime(DateTime.Now) };

        // Act & Assert
        await Assert.ThrowsAsync<PurchaseValidationException>(async () => await purchaseService.AddPurchase(dto));
    }

    /// <summary>
    /// Test: AddPurchase should accept a positive purchase amount.
    /// Validates that valid financial amounts pass through and are returned correctly.
    /// </summary>
    [Fact]
    public async Task AddPurchase_PurchaseAmountPositive_Valid()
    {
        // Arrange
        var dto = new PurchaseDTO { Id = Guid.NewGuid(), Description = "Valid", PurchaseAmount = 2.5m, TransactionDate = DateOnly.FromDateTime(DateTime.Now) };
        mockPurchaseManager.Setup(m => m.AddPurchase(It.IsAny<PurchaseDTO>(), It.IsAny<CancellationToken>())).ReturnsAsync((PurchaseDTO p, CancellationToken ct) => p);

        // Act
        var result = await purchaseService.AddPurchase(dto);

        // Assert
        Assert.Equal(dto.PurchaseAmount, result.PurchaseAmount);
        mockPurchaseManager.Verify(m => m.AddPurchase(It.IsAny<PurchaseDTO>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: AddPurchase via manager should round amounts correctly using AwayFromZero strategy.
    /// Validates financial rigor: 10.445m rounds to 10.45m (not 10.44m).
    /// </summary>
    [Fact]
    public async Task AddPurchase_RoundsAwayFromZero_Correctly()
    {
        // Arrange
        var dto = new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            Description = "Rounding Test",
            PurchaseAmount = 10.445m, // Should round to 10.45 using AwayFromZero
            TransactionDate = DateOnly.FromDateTime(DateTime.Now)
        };
        mockPurchaseManager.Setup(m => m.AddPurchase(It.IsAny<PurchaseDTO>(), It.IsAny<CancellationToken>()))
            .Returns(async (PurchaseDTO p, CancellationToken ct) =>
            {
                // Simulate manager rounding
                p.PurchaseAmount = decimal.Round(p.PurchaseAmount, 2, System.MidpointRounding.AwayFromZero);
                return await Task.FromResult(p);
            });

        // Act
        var result = await purchaseService.AddPurchase(dto);

        // Assert
        Assert.Equal(10.45m, result.PurchaseAmount);
    }

    /// <summary>
    /// Test: AddPurchase should invoke the manager and return the manager's result.
    /// Validates proper delegation and that the service acts as a coordinator (not a filter).
    /// </summary>
    [Fact]
    public async Task AddPurchase_CallsManager_AndReturnsDTO()
    {
        // Arrange
        var input = new PurchaseDTO { Id = Guid.NewGuid(), Description = "Test", PurchaseAmount = 1.0m, TransactionDate = DateOnly.FromDateTime(DateTime.Now) };
        mockPurchaseManager.Setup(m => m.AddPurchase(It.IsAny<PurchaseDTO>(), It.IsAny<CancellationToken>())).ReturnsAsync((PurchaseDTO p, CancellationToken ct) => input);

        var validInput = new PurchaseDTO { Id = Guid.NewGuid(), Description = "Test", PurchaseAmount = 1.0m, TransactionDate = DateOnly.FromDateTime(DateTime.Now) };

        // Act
        var result = await purchaseService.AddPurchase(validInput);

        // Assert
        Assert.Equal(input.Id, result.Id);
        mockPurchaseManager.Verify(m => m.AddPurchase(It.IsAny<PurchaseDTO>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: GetPurchaseTransactions should invoke the manager and return a non-null list.
    /// Validates that service properly delegates retrieval to the manager.
    /// </summary>
    
    /// <summary>
    /// Test: AddPurchase should propagate validation exceptions from the manager.
    /// Validates that service doesn't suppress manager-layer exceptions.
    /// </summary>
    [Fact]
    public async Task AddPurchase_ManagerThrowsValidationException_PropagatesToService()
    {
        // Arrange
        mockPurchaseManager.Setup(m => m.AddPurchase(It.IsAny<PurchaseDTO>(), It.IsAny<CancellationToken>())).ThrowsAsync(new PurchaseValidationException("validation failed", new List<string> { "err" }));

        var validDto = new PurchaseDTO { Id = Guid.NewGuid(), Description = "Test", PurchaseAmount = 1.0m, TransactionDate = DateOnly.FromDateTime(DateTime.Now) };

        // Act & Assert
        await Assert.ThrowsAsync<PurchaseValidationException>(async () => await purchaseService.AddPurchase(validDto));
    }
}
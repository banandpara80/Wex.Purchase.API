using Microsoft.AspNetCore.Mvc;
using Moq;
using Wex.Purchase.API.Controllers;
using Wex.Purchase.BusinessModels;
using Wex.Purchase.Service;
using Xunit;
using Serilog;
using Serilog.Core;
using Wex.Purchase.Common.Exceptions;
using System.Collections.Generic;

namespace Wex.Purchase.Unit.Tests.Controllers;

/// <summary>
/// Unit tests for PurchaseController.
/// Tests cover HTTP response codes, service delegation, and error propagation from service layer.
/// </summary>
public class PurchaseControllerTests
{
    private readonly Mock<IPurchaseService> _purchaseServiceMock;

    public PurchaseControllerTests()
    {
        _purchaseServiceMock = new Mock<IPurchaseService>();
    }

    /// <summary>
    /// Test: AddPurchase should return HTTP 201 (Created) with the created purchase.
    /// Validates correct HTTP semantics for resource creation.
    /// </summary>
    [Fact]
    public async Task AddPurchase_Returns201ResponseCode_WithCreatedPurchase()
    {
        // Arrange
        var input = new PurchaseDTO { Id = Guid.NewGuid(), Description = "Test", PurchaseAmount = 1.0m, TransactionDate = DateOnly.FromDateTime(DateTime.Now) };
        _purchaseServiceMock.Setup(s => s.AddPurchase(It.IsAny<PurchaseDTO>())).ReturnsAsync(input);

        var controller = new PurchaseController(Logger.None, _purchaseServiceMock.Object);

        // Act
        var result = await controller.AddPurchase(input);

        // Assert
        var okResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returned = Assert.IsType<PurchaseDTO>(okResult.Value);
        Assert.Equal(input.Id, returned.Id);
    }

    /// <summary>
    /// Test: AddPurchase should propagate validation exceptions from service layer to caller.
    /// Validates that service-layer errors surface to the HTTP response (handled by middleware).
    /// </summary>
    [Fact]
    public async Task AddPurchase_ServiceThrowsValidationException_PropagatesToController()
    {
        // Arrange
        _purchaseServiceMock.Setup(s => s.AddPurchase(It.IsAny<PurchaseDTO>())).ThrowsAsync(new PurchaseValidationException("validation failed", new List<string>{"err"}));

        var controller = new PurchaseController(Logger.None, _purchaseServiceMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<PurchaseValidationException>(async () => await controller.AddPurchase(new PurchaseDTO { Description = "Test" }));
    }
}

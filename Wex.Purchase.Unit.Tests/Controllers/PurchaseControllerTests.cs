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

public class PurchaseControllerTests
{
    private readonly Mock<IPurchaseService> _purchaseServiceMock;
 
    public PurchaseControllerTests()
    {
        _purchaseServiceMock = new Mock<IPurchaseService>();
    }

    [Fact]
    public async Task AddPurchase_ReturnsOk_WithCreatedPurchase()
    {
        var input = new PurchaseDTO { Id = Guid.NewGuid(), Description = "Test", PurchaseAmount = 1.0m, TransactionDate = DateOnly.FromDateTime(DateTime.Now) };
        _purchaseServiceMock.Setup(s => s.AddPurchase(It.IsAny<PurchaseDTO>())).ReturnsAsync(input);

        var controller = new PurchaseController(Logger.None, _purchaseServiceMock.Object);

        var result = await controller.AddPurchase(input);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<PurchaseDTO>(okResult.Value);
        Assert.Equal(input.Id, returned.Id);
    }

    [Fact]
    public async Task GetPurchaseTransactions_ReturnsCollection()
    {
        var request = new PurchaseRequestDTO { Ids = new Guid[] { Guid.NewGuid() }, Currency = new string[] { "USD" } };
        _purchaseServiceMock.Setup(s => s.GetPurchaseTransactions(It.IsAny<PurchaseRequestDTO>())).ReturnsAsync(new System.Collections.Generic.List<PurchaseDTO>());

        var controller = new PurchaseController(Logger.None, _purchaseServiceMock.Object);

        var result = await controller.GetPurchaseTransactionss(request);

        var actionResult = Assert.IsType<ActionResult<IEnumerable<PurchaseDTO>>>(result);
        Assert.NotNull(actionResult.Value);
    }

    [Fact]
    public async Task AddPurchase_ServiceThrowsValidationException_Propagates()
    {
        _purchaseServiceMock.Setup(s => s.AddPurchase(It.IsAny<PurchaseDTO>())).ThrowsAsync(new PurchaseValidationException("validation failed", new List<string>{"err"}));

        var controller = new PurchaseController(Logger.None, _purchaseServiceMock.Object);

        await Assert.ThrowsAsync<PurchaseValidationException>(async () => await controller.AddPurchase(new PurchaseDTO { Description = "Test" }));
    }

    [Fact]
    public async Task GetPurchaseTransactions_ServiceThrowsValidationException_Propagates()
    {
        _purchaseServiceMock.Setup(s => s.GetPurchaseTransactions(It.IsAny<PurchaseRequestDTO>())).ThrowsAsync(new PurchaseValidationException("validation failed", new List<string>{"err"}));

        var controller = new PurchaseController(Logger.None, _purchaseServiceMock.Object);

        await Assert.ThrowsAsync<PurchaseValidationException>(async () => await controller.GetPurchaseTransactionss(new PurchaseRequestDTO { Ids = new Guid[] { Guid.NewGuid() }, Currency = new string[] { "USD" } }));
    }
}

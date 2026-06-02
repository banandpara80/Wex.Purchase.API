using Moq;
using System;
using System.Threading.Tasks;
using Wex.Purchase.BusinessModels;
using Wex.Purchase.Manager;
using Wex.Purchase.Service;
using Serilog;
using Xunit;
using Serilog.Core;

namespace Wex.Purchase.Unit.Tests.Service;

public class PurchaseServiceTests
{
    private readonly Mock<IPurchaseManager> mockPurchaseManager;
    private readonly IPurchaseService purchaseService;

    public PurchaseServiceTests()
    {
        mockPurchaseManager = new Mock<IPurchaseManager>();
        purchaseService = new PurchaseService(Logger.None, mockPurchaseManager.Object);
    }

    [Fact]
    public async Task AddPurchase_CallsManager_AndReturnsDTO()
    {
        var input = new PurchaseDTO { Id = Guid.NewGuid(), Description = "Test", PurchaseAmount = 1.0m, TransactionDate = DateOnly.FromDateTime(DateTime.Now) };
        mockPurchaseManager.Setup(m => m.AddPurchase(It.IsAny<PurchaseDTO>())).ReturnsAsync(input);

        var result = await purchaseService.AddPurchase(new PurchaseDTO { Description = "Test" });

        Assert.Equal(input.Id, result.Id);
        mockPurchaseManager.Verify(m => m.AddPurchase(It.IsAny<PurchaseDTO>()), Times.Once);
    }

    [Fact]
    public async Task GetPurchaseTransactions_CallsManager_AndReturnsList()
    {
        mockPurchaseManager.Setup(m => m.GetPurchaseTransactions(It.IsAny<PurchaseRequestDTO>())).ReturnsAsync(new System.Collections.Generic.List<PurchaseDTO>());

        var result = await purchaseService.GetPurchaseTransactions(new PurchaseRequestDTO { Ids = new Guid[] { Guid.NewGuid() }, Currency = new string[] { "USD" } });

        Assert.NotNull(result);
        mockPurchaseManager.Verify(m => m.GetPurchaseTransactions(It.IsAny<PurchaseRequestDTO>()), Times.Once);
    }
}
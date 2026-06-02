using Moq;
using Wex.Purchase.BusinessModels;
using Wex.Purchase.Manager;
using Wex.Purchase.Repository;
using Wex.Purchase.Repository.Entity;
using Xunit;
using Serilog;
using Microsoft.Extensions.Logging;

namespace Wex.Purchase.Unit.Tests.Manager;

public class PurchaseManagerTests
{
    private readonly Mock<IPurchaseRepository> _purchaseRepositoryMock;
    private readonly PurchaseManager purchaseManager;

    public PurchaseManagerTests()
    {
        _purchaseRepositoryMock = new Mock<IPurchaseRepository>();
         purchaseManager = new PurchaseManager(Serilog.Core.Logger.None, _purchaseRepositoryMock.Object);

    }

    [Fact]
    public async Task AddPurchase_MapsAndCallsRepository()
    {
         
        _purchaseRepositoryMock.Setup(r => r.AddAsync(It.IsAny<PurchaseBO>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

         var dto = new PurchaseDTO { Id = Guid.NewGuid(), Description = "X", PurchaseAmount = 2.0m, TransactionDate = DateOnly.FromDateTime(DateTime.Now) };

        var result = await purchaseManager.AddPurchase(dto);

        Assert.NotNull(result);
        _purchaseRepositoryMock.Verify(r => r.AddAsync(It.IsAny<PurchaseBO>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPurchaseTransactions_ReturnsMappedList()
    { 
        var bos = new List<PurchaseBO> { new PurchaseBO { Id = Guid.NewGuid(), Description = "X", PurchaseAmount = 3.3m, TransactionDate = DateOnly.FromDateTime(DateTime.Now) } };
        _purchaseRepositoryMock.Setup(r => r.GetPurchaseTransactions(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>())).ReturnsAsync(bos);

        var dtoList = await purchaseManager.GetPurchaseTransactions(new PurchaseRequestDTO { Ids = new Guid[] { bos[0].Id }, Currency = new string[] { "USD" } });

        Assert.NotNull(dtoList);
        _purchaseRepositoryMock.Verify(r => r.GetPurchaseTransactions(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
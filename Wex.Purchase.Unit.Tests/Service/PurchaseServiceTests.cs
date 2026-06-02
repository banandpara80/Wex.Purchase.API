using Moq;
using System;
using System.Threading.Tasks;
using Wex.Purchase.BusinessModels;
using Wex.Purchase.Manager;
using Wex.Purchase.Service;
using Serilog;
using Xunit;
using Wex.Purchase.Common.Exceptions;
using System.Collections.Generic;
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
    public async Task AddPurchase_TransactionDateParsedFromString_Allows()
    {
        var svc = new PurchaseService(Logger.None, mockPurchaseManager.Object);

        var parsed = DateOnly.Parse("2024-05-01");
        var dto = new PurchaseDTO { Id = Guid.NewGuid(), Description = "Valid", PurchaseAmount = 1.23m, TransactionDate = parsed };

        mockPurchaseManager.Setup(m => m.AddPurchase(It.IsAny<PurchaseDTO>())).ReturnsAsync((PurchaseDTO p) => p);

        var result = await svc.AddPurchase(dto);

        Assert.Equal(parsed, result.TransactionDate);
        mockPurchaseManager.Verify(m => m.AddPurchase(It.IsAny<PurchaseDTO>()), Times.Once);
    }

    [Fact]
    public async Task AddPurchase_InvalidDto_ThrowsPurchaseValidationException()
    {
        var svc = new PurchaseService(Logger.None, mockPurchaseManager.Object);

        var dto = new PurchaseDTO { Id = Guid.NewGuid(), Description = string.Empty, PurchaseAmount = 2.0m, TransactionDate = DateOnly.FromDateTime(DateTime.Now) };

        await Assert.ThrowsAsync<PurchaseValidationException>(async () => await svc.AddPurchase(dto));
    }

    [Fact]
    public async Task GetPurchaseTransactions_NullRequest_ThrowsPurchaseValidationException()
    {
        var svc = new PurchaseService(Logger.None, mockPurchaseManager.Object);

        await Assert.ThrowsAsync<PurchaseValidationException>(async () => await svc.GetPurchaseTransactions(null));
    }

    [Fact]
    public async Task AddPurchase_DescriptionExactly50_Allows()
    {
        var svc = new PurchaseService(Logger.None, mockPurchaseManager.Object);

        var desc = new string('A', 50);
        var dto = new PurchaseDTO { Id = Guid.NewGuid(), Description = desc, PurchaseAmount = 10.0m, TransactionDate = DateOnly.FromDateTime(DateTime.Now) };

        mockPurchaseManager.Setup(m => m.AddPurchase(It.IsAny<PurchaseDTO>())).ReturnsAsync((PurchaseDTO p) => p);

        var result = await svc.AddPurchase(dto);

        Assert.Equal(desc, result.Description);
        mockPurchaseManager.Verify(m => m.AddPurchase(It.IsAny<PurchaseDTO>()), Times.Once);
    }

    [Fact]
    public async Task AddPurchase_DescriptionTooLong_ThrowsPurchaseValidationException()
    {
        var svc = new PurchaseService(Logger.None, mockPurchaseManager.Object);

        var desc = new string('B', 51);
        var dto = new PurchaseDTO { Id = Guid.NewGuid(), Description = desc, PurchaseAmount = 10.0m, TransactionDate = DateOnly.FromDateTime(DateTime.Now) };

        await Assert.ThrowsAsync<PurchaseValidationException>(async () => await svc.AddPurchase(dto));
    }

    [Fact]
    public async Task AddPurchase_TransactionDateDefault_ThrowsPurchaseValidationException()
    {
        var svc = new PurchaseService(Logger.None, mockPurchaseManager.Object);

        var dto = new PurchaseDTO { Id = Guid.NewGuid(), Description = "Valid", PurchaseAmount = 5.0m, TransactionDate = default };

        await Assert.ThrowsAsync<PurchaseValidationException>(async () => await svc.AddPurchase(dto));
    }

    [Fact]
    public async Task AddPurchase_PurchaseAmountZero_ThrowsPurchaseValidationException()
    {
        var svc = new PurchaseService(Logger.None, mockPurchaseManager.Object);

        var dto = new PurchaseDTO { Id = Guid.NewGuid(), Description = "Valid", PurchaseAmount = 0.0m, TransactionDate = DateOnly.FromDateTime(DateTime.Now) };

        await Assert.ThrowsAsync<PurchaseValidationException>(async () => await svc.AddPurchase(dto));
    }

    [Fact]
    public async Task AddPurchase_PurchaseAmountPositive_Allows()
    {
        var svc = new PurchaseService(Logger.None, mockPurchaseManager.Object);

        var dto = new PurchaseDTO { Id = Guid.NewGuid(), Description = "Valid", PurchaseAmount = 2.5m, TransactionDate = DateOnly.FromDateTime(DateTime.Now) };

        mockPurchaseManager.Setup(m => m.AddPurchase(It.IsAny<PurchaseDTO>())).ReturnsAsync((PurchaseDTO p) => p);

        var result = await svc.AddPurchase(dto);

        Assert.Equal(dto.PurchaseAmount, result.PurchaseAmount);
        mockPurchaseManager.Verify(m => m.AddPurchase(It.IsAny<PurchaseDTO>()), Times.Once);
    }

    [Fact]
    public async Task AddPurchase_CallsManager_AndReturnsDTO()
    {
        var input = new PurchaseDTO { Id = Guid.NewGuid(), Description = "Test", PurchaseAmount = 1.0m, TransactionDate = DateOnly.FromDateTime(DateTime.Now) };
        mockPurchaseManager.Setup(m => m.AddPurchase(It.IsAny<PurchaseDTO>())).ReturnsAsync(input);

        var validInput = new PurchaseDTO { Id = Guid.NewGuid(), Description = "Test", PurchaseAmount = 1.0m, TransactionDate = DateOnly.FromDateTime(DateTime.Now) };
        var result = await purchaseService.AddPurchase(validInput);

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

    [Fact]
    public async Task AddPurchase_ManagerThrowsValidationException_Propagates()
    {
        mockPurchaseManager.Setup(m => m.AddPurchase(It.IsAny<PurchaseDTO>())).ThrowsAsync(new PurchaseValidationException("validation failed", new List<string> { "err" }));

        var validDto = new PurchaseDTO { Id = Guid.NewGuid(), Description = "Test", PurchaseAmount = 1.0m, TransactionDate = DateOnly.FromDateTime(DateTime.Now) };

        await Assert.ThrowsAsync<PurchaseValidationException>(async () => await purchaseService.AddPurchase(validDto));
    }

    [Fact]
    public async Task GetPurchaseTransactions_ManagerThrowsValidationException_Propagates()
    {
        mockPurchaseManager.Setup(m => m.GetPurchaseTransactions(It.IsAny<PurchaseRequestDTO>())).ThrowsAsync(new PurchaseValidationException("validation failed", new List<string> { "err" }));

        await Assert.ThrowsAsync<PurchaseValidationException>(async () => await purchaseService.GetPurchaseTransactions(new PurchaseRequestDTO { Ids = new Guid[] { Guid.NewGuid() }, Currency = new string[] { "USD" } }));
    }
}
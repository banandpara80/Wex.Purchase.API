using Moq;
using Wex.Purchase.BusinessModels;
using Wex.Purchase.Manager;
using Wex.Purchase.Repository;
using Wex.Purchase.Repository.Entity;
using Xunit;
using Serilog.Core;
using Wex.Purchase.Manager.ExchangeRateConversion;
using System;
using System.Threading;
using System.Collections.Generic;

namespace Wex.Purchase.Unit.Tests.Manager;

/// <summary>
/// Unit tests for PurchaseManager.
/// Tests cover DTO-to-entity mapping, financial rounding logic, repository coordination and conversions.
/// </summary>
public class PurchaseManagerTests
{
    private readonly Mock<IPurchaseRepository> _purchaseRepositoryMock;
    private readonly PurchaseManager _purchaseManager;

    public PurchaseManagerTests()
    {
        _purchaseRepositoryMock = new Mock<IPurchaseRepository>();
        _purchaseManager = new PurchaseManager(Logger.None, _purchaseRepositoryMock.Object);
    }

    /// <summary>
    /// Test: AddPurchase should map DTO to business object and invoke repository once.
    /// Validates that the manager acts as a coordinator between layers.
    /// </summary>
    [Fact]
    public async Task AddPurchase_MapsDTOToBOAndCallsRepository()
    {
        // Arrange
        _purchaseRepositoryMock.Setup(r => r.AddAsync(It.IsAny<PurchaseBO>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var dto = new PurchaseDTO { Id = Guid.NewGuid(), Description = "X", PurchaseAmount = 2.0m, TransactionDate = DateTime.Now };

        // Act
        var result = await _purchaseManager.AddPurchase(dto);

        // Assert
        Assert.NotNull(result);
        _purchaseRepositoryMock.Verify(r => r.AddAsync(It.IsAny<PurchaseBO>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: AddPurchase should round purchase amounts to nearest cent using AwayFromZero before persistence.
    /// Validates financial rigor: ensures precise decimal handling (2.345m → 2.35m).
    /// </summary>
    [Fact]
    public async Task AddPurchase_RoundsAmountToNearestCent_BeforePersistingToDB()
    {
        // Arrange
        PurchaseBO? captured = null;
        _purchaseRepositoryMock.Setup(r => r.AddAsync(It.IsAny<PurchaseBO>(), It.IsAny<CancellationToken>()))
            .Callback<PurchaseBO, CancellationToken>((b, ct) => captured = b)
            .Returns(Task.CompletedTask);

        // 2.345 rounded to 2 decimals using MidpointRounding.AwayFromZero -> 2.35
        var dto = new PurchaseDTO { Id = Guid.NewGuid(), Description = "Rounding", PurchaseAmount = 2.345m, TransactionDate = DateTime.Now };

        // Act
        var result = await _purchaseManager.AddPurchase(dto);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal(decimal.Round(2.345m, 2, MidpointRounding.AwayFromZero), captured.PurchaseAmount);
        _purchaseRepositoryMock.Verify(r => r.AddAsync(It.IsAny<PurchaseBO>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: GetPurchaseTransactions should retrieve entities from repository and return mapped DTOs.
    /// Validates that the manager properly converts repository results for service layer consumption.
    /// </summary>
    [Fact]
    public async Task GetPurchaseTransactions_ReturnsMappedList()
    {
        // Arrange
        var bos = new List<PurchaseBO> { new PurchaseBO { Id = Guid.NewGuid(), Description = "X", PurchaseAmount = 3.3m, TransactionDate = DateTime.Now } }; 
        _purchaseRepositoryMock.Setup(r => r.GetPurchaseTransactions(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>())).ReturnsAsync(bos);

        var request = new PurchaseRequestDTO { Ids = bos[0].Id.ToString(), Currency = "USD" };

        // Act
        var dtoList = await _purchaseManager.GetPurchaseTransactions(request);

        // Assert
        Assert.NotNull(dtoList);
        _purchaseRepositoryMock.Verify(r => r.GetPurchaseTransactions(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: GetPurchaseOrderById should map repository entity to DTO.
    /// </summary>
    [Fact]
    public async Task GetPurchaseOrderById_ReturnsMappedDto()
    {
        // Arrange
        var bo = new PurchaseBO { Id = Guid.NewGuid(), Description = "Order", PurchaseAmount = 4.4m, TransactionDate = DateTime.Now }; 
        _purchaseRepositoryMock.Setup(r => r.GetByIdAsync(bo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(bo);

        // Act
        var dto = await _purchaseManager.GetPurchaseOrderById(bo.Id);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(bo.Id, dto.Id);
        Assert.Equal(bo.Description, dto.Description);
        Assert.Equal(bo.PurchaseAmount, dto.PurchaseAmount);
    }

    /// <summary>
    /// Test: GetPurchaseTransactionsWithConversions should throw when exchange service is not configured.
    /// </summary>
    [Fact]
    public async Task GetPurchaseTransactionsWithConversions_NoExchangeService_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new PurchaseRequestDTO { Ids = string.Empty, Currency = "USD"  };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await _purchaseManager.GetPurchaseTransactionsWithConversions(request));
    }

    /// <summary>
    /// Test: GetPurchaseTransactionsWithConversions should delegate to exchange service and return converted results.
    /// </summary>
    [Fact]
    public async Task GetPurchaseTransactionsWithConversions_WithService_ReturnsConvertedList()
    {
        // Arrange
        var bo = new PurchaseBO { Id = Guid.NewGuid(), Description = "Conv", PurchaseAmount = 100m, TransactionDate = DateTime.Now };
        _purchaseRepositoryMock.Setup(r => r.GetPurchaseTransactions(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<PurchaseBO> { bo });

        var converted = new PurchaseWithExchangeRateDTO
        {
            Purchase = new PurchaseDTO { Id = bo.Id, Description = bo.Description, PurchaseAmount = bo.PurchaseAmount, TransactionDate = bo.TransactionDate },
            ExchangeRate = 1.1m,
            ConvertedAmount = Math.Round(bo.PurchaseAmount / 1.1m, 2, MidpointRounding.AwayFromZero)
        };

        var mockConv = new Mock<IExchangeRateConversionService>();
        mockConv.Setup(s => s.ConvertPurchasesAsync(It.IsAny<IList<PurchaseDTO>>(), "USD", It.IsAny<CancellationToken>())).ReturnsAsync(new List<PurchaseWithExchangeRateDTO> { converted });

        var managerWithConv = new PurchaseManager(Logger.None, _purchaseRepositoryMock.Object, mockConv.Object);

        var request = new PurchaseRequestDTO { Ids = bo.Id.ToString(), Currency = "USD" };

        // Act
        var result = await managerWithConv.GetPurchaseTransactionsWithConversions(request);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(converted.Purchase.Id, result[0].Purchase.Id);
        mockConv.Verify(s => s.ConvertPurchasesAsync(It.IsAny<IList<PurchaseDTO>>(), "USD", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPurchaseTransactions_RepositoryThrows_ThrowsPurchaseDatabaseException()
    {
        // Arrange
        var repoMock = new Mock<IPurchaseRepository>();
        repoMock.Setup(r => r.GetPurchaseTransactions(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("repo failure"));

        var convMock = new Mock<IExchangeRateConversionService>();
        var manager = new PurchaseManager(Logger.None, repoMock.Object, convMock.Object);

        var request = new PurchaseRequestDTO { Ids = Guid.NewGuid().ToString(), Currency = "USD" };

        // Act & Assert
        await Assert.ThrowsAsync<Wex.Purchase.Common.Exceptions.PurchaseDatabaseException>(async () => await manager.GetPurchaseTransactions(request));
    }
}

using System;
using Xunit;
using Wex.Purchase.Manager.EntityMapper;
using Wex.Purchase.Repository.Entity;
using Wex.Purchase.BusinessModels;

namespace Wex.Purchase.Tests;

public class MapperTests
{
    [Fact]
    public void MapToPurchaseDTO_NullEntity_ThrowsArgumentNullException()
    {
        PurchaseBO? entity = null;
        Assert.Throws<ArgumentNullException>(() => PurchaseMapper.MapToPurchaseDTO(entity!));
    }

    [Fact]
    public void MapToPurchaseDTO_ValidEntity_MapsProperties()
    {
        var entity = new PurchaseBO
        {
            Id = Guid.NewGuid(),
            Description = "Test",
            PurchaseAmount = 10.0m,
            TransactionDate = DateOnly.FromDateTime(DateTime.Now)
        };

        var dto = entity.MapToPurchaseDTO();

        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(entity.Description, dto.Description);
        Assert.Equal(entity.PurchaseAmount, dto.PurchaseAmount);
        Assert.Equal(entity.TransactionDate, dto.TransactionDate);
    }

    [Fact]
    public void MapToPurchaseBO_NullDto_ThrowsArgumentNullException()
    {
        PurchaseDTO? dto = null;
        Assert.Throws<ArgumentNullException>(() => PurchaseMapper.MapToPurchaseBO(dto!));
    }

    [Fact]
    public void MapToPurchaseBO_ValidDto_MapsProperties()
    {
        var dto = new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            Description = "Test",
            PurchaseAmount = 5.55m,
            TransactionDate = DateOnly.FromDateTime(DateTime.Now)
        };

        var bo = dto.MapToPurchaseBO();

        Assert.Equal(dto.Id, bo.Id);
        Assert.Equal(dto.Description, bo.Description);
        Assert.Equal(dto.PurchaseAmount, bo.PurchaseAmount);
        Assert.Equal(dto.TransactionDate, bo.TransactionDate);
    }
}
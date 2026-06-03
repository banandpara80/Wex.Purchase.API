using System;
using Xunit;
using Wex.Purchase.Repository.Entity;
using Wex.Purchase.BusinessModels;

namespace Wex.Purchase.Manager.EntityMapper.Tests;

/// <summary>
/// Unit tests for PurchaseMapper.
/// Tests cover null-safety and bidirectional DTO ↔ Entity mapping with property preservation.
/// </summary>
public class MapperTests
{
    /// <summary>
    /// Test: MapToPurchaseDTO should reject null entity input.
    /// Validates null-safety and defensive programming at the mapping layer.
    /// </summary>
    [Fact]
    public void MapToPurchaseDTO_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        PurchaseBO? entity = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PurchaseMapper.MapToPurchaseDTO(entity!));
    }

    /// <summary>
    /// Test: MapToPurchaseDTO should map all entity properties to DTO correctly.
    /// Validates that no data is lost or corrupted during the entity-to-DTO transformation.
    /// </summary>
    [Fact]
    public void MapToPurchaseDTO_ValidEntity_MapsProperties()
    {
        // Arrange
        var entity = new PurchaseBO
        {
            Id = Guid.NewGuid(),
            Description = "Test",
            PurchaseAmount = 10.0m,
            TransactionDate = DateOnly.FromDateTime(DateTime.Now)
        };

        // Act
        var dto = entity.MapToPurchaseDTO();

        // Assert
        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(entity.Description, dto.Description);
        Assert.Equal(entity.PurchaseAmount, dto.PurchaseAmount);
        Assert.Equal(entity.TransactionDate, dto.TransactionDate);
    }

    /// <summary>
    /// Test: MapToPurchaseBO should reject null DTO input.
    /// Validates null-safety and defensive programming at the mapping layer.
    /// </summary>
    [Fact]
    public void MapToPurchaseBO_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        PurchaseDTO? dto = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PurchaseMapper.MapToPurchaseBO(dto!));
    }

    /// <summary>
    /// Test: MapToPurchaseBO should map all DTO properties to entity correctly.
    /// Validates that no data is lost or corrupted during the DTO-to-entity transformation.
    /// </summary>
    [Fact]
    public void MapToPurchaseBO_ValidDto_MapsProperties()
    {
        // Arrange
        var dto = new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            Description = "Test",
            PurchaseAmount = 5.55m,
            TransactionDate = DateOnly.FromDateTime(DateTime.Now)
        };

        // Act
        var bo = dto.MapToPurchaseBO();

        // Assert
        Assert.Equal(dto.Id, bo.Id);
        Assert.Equal(dto.Description, bo.Description);
        Assert.Equal(dto.PurchaseAmount, bo.PurchaseAmount);
        Assert.Equal(dto.TransactionDate, bo.TransactionDate);
    }
}
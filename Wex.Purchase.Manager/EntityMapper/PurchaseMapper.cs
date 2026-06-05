using Wex.Purchase.BusinessModels;
using Wex.Purchase.Repository.Entity;

namespace Wex.Purchase.Manager.EntityMapper;

/// <summary>
/// Mapper class for converting between Purchase entity and PurchaseDTO.
/// Provides extension methods for object-to-object mapping.
/// </summary>
public static class PurchaseMapper
{
    /// <summary>
    /// Maps a Purchase entity to a PurchaseDTO.
    /// </summary>
    /// <param name="purchase">The Purchase entity object.</param>
    /// <returns>PurchaseDTO with mapped properties from the entity.</returns>
    /// <exception cref="ArgumentNullException">Thrown when purchase is null.</exception>
    public static PurchaseDTO MapToPurchaseDTO(this PurchaseBO purchase)
    {
        if (purchase == null)
            throw new ArgumentNullException(nameof(purchase), "Purchase entity cannot be null.");

        return new PurchaseDTO
        {
            Id = purchase.Id,
            Description = purchase.Description,
            PurchaseAmount = purchase.PurchaseAmount,
            TransactionDate = purchase.TransactionDate,
            ExchangeRateDate = purchase.ExchangeRateDate            
        };
    }

    /// <summary>
    /// Maps a collection of Purchase entities to a collection of PurchaseDTO.
    /// </summary>
    /// <param name="purchases">Collection of Purchase entities.</param>
    /// <returns>Enumerable of PurchaseDTO objects.</returns>
    /// <exception cref="ArgumentNullException">Thrown when purchases collection is null.</exception>
    public static IList<PurchaseDTO> MapToPurchaseDTOs(this IList<PurchaseBO> purchases)
    {
        if (purchases == null)
            throw new ArgumentNullException(nameof(purchases), "Purchases collection cannot be null.");

        return purchases.Select(p => p.MapToPurchaseDTO()).ToList<PurchaseDTO>();
    }

    /// <summary>
    /// Maps a PurchaseDTO back to a Purchase entity.
    /// </summary>
    /// <param name="dto">The PurchaseDTO to map.</param>
    /// <returns>Purchase entity with mapped properties.</returns>
    /// <exception cref="ArgumentNullException">Thrown when dto is null.</exception>
    public static PurchaseBO MapToPurchaseBO(this PurchaseDTO dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto), "PurchaseDTO cannot be null.");

        return new PurchaseBO
        {
            Id = dto.Id,
            Description = dto.Description,
            PurchaseAmount = dto.PurchaseAmount,
            TransactionDate = dto.TransactionDate,
            ExchangeRateDate = dto.ExchangeRateDate
        };
    }
}

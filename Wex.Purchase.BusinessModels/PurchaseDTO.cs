namespace Wex.Purchase.BusinessModels;

/// <summary>
/// Data Transfer Object for purchase information.
/// Used for transferring purchase data between API layers.
/// </summary>
public class PurchaseDTO
{
    /// <summary>
    /// Gets or sets the unique identifier of the purchase.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the description of the purchase.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Gets or sets the transaction date of the purchase.
    /// </summary>
    public DateOnly TransactionDate { get; set; }

    /// <summary>
    /// Gets or sets the purchase amount.
    /// </summary>
    public decimal PurchaseAmount { get; set; }
}

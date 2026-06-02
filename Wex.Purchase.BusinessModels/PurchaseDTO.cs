using System.ComponentModel.DataAnnotations;

namespace Wex.Purchase.BusinessModels;

/// <summary>
/// Data Transfer Object for purchase information.
/// Used for transferring purchase data between API layers.
/// </summary>
public class PurchaseDTO : System.ComponentModel.DataAnnotations.IValidatableObject
{
    /// <summary>
    /// Gets or sets the unique identifier of the purchase.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the description of the purchase.
    /// </summary>
    [Required]
    [StringLength(50, ErrorMessage = "Description cannot exceed 50 characters.")]
    public required string Description { get; set; }

    /// <summary>
    /// Gets or sets the transaction date of the purchase.
    /// </summary>
    [Required]
    [DataType(DataType.Date)]
    public DateOnly TransactionDate { get; set; }

    /// <summary>
    /// Gets or sets the purchase amount.
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Purchase amount must be greater than zero.")]
    public decimal PurchaseAmount { get; set; }

    public IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(System.ComponentModel.DataAnnotations.ValidationContext validationContext)
    {
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        // TransactionDate must be provided (not default)
        if (TransactionDate == default)
        {
            results.Add(new System.ComponentModel.DataAnnotations.ValidationResult("TransactionDate is required", new[] { nameof(TransactionDate) }));
        }

        // PurchaseAmount must be positive
        if (PurchaseAmount <= 0)
        {
            results.Add(new System.ComponentModel.DataAnnotations.ValidationResult("PurchaseAmount must be greater than zero", new[] { nameof(PurchaseAmount) }));
        }
        
        return results;
    }
}

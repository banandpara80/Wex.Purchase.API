using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Wex.Purchase.BusinessModels.Validators;

namespace Wex.Purchase.BusinessModels;

/// <summary>
/// Data Transfer Object for purchase information.
/// Used for transferring purchase data between API layers.
/// </summary>
public class PurchaseDTO : IValidatableObject
{
    /// <summary>
    /// Gets or sets the unique identifier of the purchase.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the description of the purchase.
    /// </summary>
    [Required(ErrorMessage = "Description is required.")]
    [StringLength(50, ErrorMessage = "Description cannot exceed 50 characters.")]
    [RegularExpression(@"^[A-Za-z0-9\-\(\),/&\s]+$", ErrorMessage = "Description contains invalid characters. Only letters, numbers, spaces and the characters - ( , ) / & are allowed.")]
    public required string Description { get; set; }

    /// <summary>
    /// Gets or sets the transaction date of the purchase.
    /// </summary>
    [Required(ErrorMessage = "TransactionDate is required")]
    [NotFutureDate]
    public DateTime TransactionDate { get; set; }

    /// <summary>
    /// Gets or sets the purchase amount.
    /// </summary>
    [Required(ErrorMessage = "Please provide the purchase amount")]
    [Range(0.01, 999999999.99, ErrorMessage = "Purchase amount must be greater than zero.")]
    public decimal PurchaseAmount { get; set; }

    /// <summary>
    /// Gets or sets only the date component of transaction date of the purchase.
    /// </summary>
    [JsonIgnore]
    public DateOnly ExchangeRateDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        if (TransactionDate == default)
        {
            results.Add(new ValidationResult("TransactionDate is required", new[] { nameof(TransactionDate) }));
        }

        if (!DateTime.TryParse(TransactionDate.ToString(), out _))
        {
            results.Add(new ValidationResult("Please provide a valid transaction date", new[] { nameof(TransactionDate) }));
        }
       
        return results;
    }
}

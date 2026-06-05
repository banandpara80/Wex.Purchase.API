using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Wex.Purchase.BusinessModels
{
    /// <summary>
    /// Data Transfer Object for purchase query requests.
    /// Contains filtering criteria for retrieving specific purchase transactions.
    /// </summary>
    public class PurchaseRequestDTO : IValidatableObject
    {

        /// <summary>
        /// Gets or sets the array of purchase IDs to retrieve.
        /// </summary>
        [Required(ErrorMessage = "Please provide purchase ids")]
        public required string Ids { get; set; }

        /// <summary>
        /// Gets or sets the currency codes for filtering.
        /// </summary>
        [Required(ErrorMessage = "Please provide the target currency")]
        public required string Currency { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
           string[] idArray = Ids.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var id in idArray)
            {
                if (!Guid.TryParse(id.Trim(), out _))
                {
                    yield return new ValidationResult($"Invalid purchase ID: {id}. Each ID must be a valid GUID.", new[] { nameof(Ids) });
                }
            }
        }
    }
}

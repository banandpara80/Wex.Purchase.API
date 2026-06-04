using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Wex.Purchase.Repository.Entity
{
    /// <summary>
    /// Business object representing a purchase transaction.
    /// Contains core purchase data including amount, description, and transaction date.
    /// </summary>
    public class PurchaseBO
    {
        /// <summary>
        /// Gets or sets the unique identifier of the purchase.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the description of the purchase.
        /// </summary>
        [System.ComponentModel.DataAnnotations.StringLength(50, ErrorMessage = "Description cannot exceed 50 characters.")]
        [System.ComponentModel.DataAnnotations.Required]
        public required string Description { get; set; }

        /// <summary>
        /// Gets or sets the transaction date of the purchase.
        /// </summary>
        public DateTime TransactionDate { get; set; }

        /// <summary>
        /// Gets or sets the purchase amount.
        /// </summary>
        public decimal PurchaseAmount { get; set; }
    }
}

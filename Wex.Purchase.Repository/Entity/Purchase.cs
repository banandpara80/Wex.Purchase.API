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
        [Key]
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
}

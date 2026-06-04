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
    public class PurchaseRequestDTO
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
    }
}

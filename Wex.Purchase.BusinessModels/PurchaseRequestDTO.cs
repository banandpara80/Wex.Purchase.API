using System;
using System.Collections.Generic;
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
        public string[] Ids { get; set; }

        /// <summary>
        /// Gets or sets the array of currency codes for filtering.
        /// </summary>
        public string[] Currency { get; set; }
    }
}

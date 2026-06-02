using System;
using System.Collections.Generic;
using System.Text;
using Wex.Purchase.BusinessModels;

namespace Wex.Purchase.Service
{
    /// <summary>
    /// Service interface for managing purchase operations.
    /// Defines contracts for adding and retrieving purchase transactions.
    /// </summary>
    public interface IPurchaseService
    {
        /// <summary>
        /// Adds a new purchase to the system.
        /// </summary>
        /// <param name="purchaseDTO">The purchase data to add.</param>
        /// <returns>The added purchase with generated metadata.</returns>
        Task<PurchaseDTO> AddPurchase(PurchaseDTO purchaseDTO);

        /// <summary>
        /// Retrieves purchase transactions based on specified criteria.
        /// </summary>
        /// <param name="purchaseRequestDTO">The filtering criteria for purchases.</param>
        /// <returns>A collection of purchase DTOs matching the criteria.</returns>
        Task<IList<PurchaseDTO>> GetPurchaseTransactions(PurchaseRequestDTO purchaseRequestDTO);


        /// <summary>
        /// Retrieves purchase transaction based on order id.
        /// </summary>
        /// <param name="id">Purchase Id</param>
        /// <returns>Purchase DTO matching the criteria.</returns>
        Task<PurchaseDTO> GetPurchaseOrderById(Guid id);
    }
}

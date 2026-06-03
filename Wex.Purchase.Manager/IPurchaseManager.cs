using System;
using System.Collections.Generic;
using System.Text;
using Wex.Purchase.BusinessModels;

namespace Wex.Purchase.Manager
{
    /// <summary>
    /// Manager interface for purchase business logic operations.
    /// Coordinates between service and repository layers for purchase management.
    /// Supports exchange rate conversions via Treasury API integration.
    /// </summary>
    public interface IPurchaseManager
    {
        /// <summary>
        /// Adds a new purchase to the system.
        /// </summary>
        /// <param name="purchaseDTO">The purchase data to add.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>The added purchase with generated metadata.</returns>
        Task<PurchaseDTO> AddPurchase(PurchaseDTO purchaseDTO, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves purchase transaction based on order id.
        /// </summary>
        /// <param name="id">Purchase Id</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>Purchase DTO matching the criteria.</returns>
        Task<PurchaseDTO> GetPurchaseOrderById(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves purchase transactions based on specified criteria.
        /// </summary>
        /// <param name="purchaseRequestDTO">The filtering criteria for purchases.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A collection of purchase DTOs matching the criteria.</returns>
        Task<IList<PurchaseDTO>> GetPurchaseTransactions(PurchaseRequestDTO purchaseRequestDTO, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves purchase transactions with exchange rate conversions to specified currencies.
        /// Uses Treasury Reporting Rates of Exchange API for current conversion rates.
        /// </summary>
        /// <param name="purchaseRequestDTO">The filtering criteria for purchases.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A collection of purchases with exchange rate conversion information.</returns>
        Task<IList<PurchaseWithExchangeRateDTO>> GetPurchaseTransactionsWithConversions(PurchaseRequestDTO purchaseRequestDTO, CancellationToken cancellationToken = default);
    }
}

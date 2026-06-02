using System;
using System.Collections.Generic;
using System.Text;
using Wex.Purchase.Repository.Entity;

namespace Wex.Purchase.Repository
{
    /// <summary>
    /// Repository interface for managing purchase data access operations.
    /// Defines contracts for adding and retrieving purchase entities.
    /// </summary>
    public interface IPurchaseRepository
    {
        /// <summary>
        /// Retrieves all purchase entities from the database.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>An enumerable collection of all purchases.</returns>
        Task<IEnumerable<PurchaseBO>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a purchase entity by its ID.
        /// </summary>
        /// <param name="id">The unique identifier of the purchase.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>The purchase entity if found; otherwise null.</returns>
        Task<PurchaseBO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new purchase entity to the database.
        /// </summary>
        /// <param name="purchase">The purchase entity to add.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddAsync(PurchaseBO purchase, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves purchase transactions matching the specified IDs.
        /// </summary>
        /// <param name="ids">Array of purchase IDs to retrieve.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A list of purchase entities matching the specified IDs.</returns>
        Task<IList<PurchaseBO>> GetPurchaseTransactions(Guid[] ids, CancellationToken cancellationToken = default);
    }
}

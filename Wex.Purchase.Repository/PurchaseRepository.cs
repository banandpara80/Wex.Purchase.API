using Microsoft.EntityFrameworkCore;
using Wex.Purchase.Repository.Entity;

namespace Wex.Purchase.Repository
{
    /// <summary>
    /// Repository implementation for accessing purchase data from the database.
    /// Provides methods for CRUD operations on purchase entities using Entity Framework Core.
    /// </summary>
    public class PurchaseRepository : IPurchaseRepository
    {
        private readonly PurchaseDbContext _db;

        /// <summary>
        /// Initializes a new instance of the PurchaseRepository class.
        /// </summary>
        /// <param name="db">The database context for purchase operations.</param>
        public PurchaseRepository(PurchaseDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Adds a new purchase to the database.
        /// </summary>
        /// <param name="purchase">The purchase entity to add.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AddAsync(PurchaseBO purchase, CancellationToken cancellationToken = default)
        {
            await _db.Purchases.AddAsync(purchase, cancellationToken);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw;
            }

            await GetByIdAsync(purchase.Id, cancellationToken);
        }

        /// <summary>
        /// Retrieves a purchase by its ID.
        /// </summary>
        /// <param name="id">The unique identifier of the purchase.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>The purchase entity if found; otherwise null.</returns>
        public async Task<PurchaseBO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _db.Purchases.FindAsync(new object[] { id }, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Retrieves all purchases from the database.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>An enumerable collection of all purchases.</returns>
        public async Task<IEnumerable<PurchaseBO>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Purchases.ToListAsync<PurchaseBO>(cancellationToken);
        }

        /// <summary>
        /// Retrieves purchase transactions matching the specified IDs.
        /// </summary>
        /// <param name="ids">Array of purchase IDs to retrieve.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A list of purchase entities matching the specified IDs.</returns>
        public async Task<IList<PurchaseBO>> GetPurchaseTransactions(Guid[] ids, CancellationToken cancellationToken = default)
        {
            return await _db.Purchases.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);
        }
    }
}

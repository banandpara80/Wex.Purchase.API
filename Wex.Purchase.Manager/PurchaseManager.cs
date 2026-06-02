using Wex.Purchase.BusinessModels;
using Wex.Purchase.Manager.EntityMapper;
using Wex.Purchase.Repository;
using Wex.Purchase.Repository.Entity;
using Wex.Purchase.Common.Exceptions;
using Serilog;

namespace Wex.Purchase.Manager
{
    /// <summary>
    /// Manager implementation for purchase business logic operations.
    /// Handles mapping between DTOs and entities, coordinates data operations.
    /// </summary>
    public class PurchaseManager : IPurchaseManager
    {
        private readonly IPurchaseRepository purchaseRepository;
        private readonly ILogger Logger;

        /// <summary>
        /// Initializes a new instance of the PurchaseManager class.
        /// </summary>
        /// <param name="purchaseRepository">The repository for data access operations.</param>
        public PurchaseManager(ILogger logger, IPurchaseRepository purchaseRepository) { 
            Logger = logger;
            this.purchaseRepository = purchaseRepository;
        }

        /// <summary>
        /// Adds a new purchase to the system.
        /// </summary>
        /// <param name="purchaseDTO">The purchase data to add.</param>
        /// <returns>The added purchase with generated metadata.</returns>
        public async Task<PurchaseDTO> AddPurchase(PurchaseDTO purchaseDTO)
        {
            // Ensure PurchaseAmount is rounded to nearest cent before persisting (AwayFromZero)
            purchaseDTO.PurchaseAmount = decimal.Round(purchaseDTO.PurchaseAmount, 2, MidpointRounding.AwayFromZero);

            PurchaseBO purchaseBO = PurchaseMapper.MapToPurchaseBO(purchaseDTO);

            try
            {
                await purchaseRepository.AddAsync(purchaseBO, new CancellationToken());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving purchase to repository");
                throw new PurchaseDatabaseException("Failed to save purchase", ex);
            }

            purchaseDTO = purchaseBO.MapToPurchaseDTO();

            return purchaseDTO;
        }

        public async Task<PurchaseDTO> GetPurchaseOrderById(Guid id)
        {
            PurchaseBO purchaseBO = await purchaseRepository.GetByIdAsync(id, new CancellationToken());

            PurchaseDTO purchaseDTO = PurchaseMapper.MapToPurchaseDTO(purchaseBO);

            return purchaseDTO;
        }

        /// <summary>
        /// Retrieves purchase transactions based on specified criteria.
        /// </summary>
        /// <param name="purchaseRequestDTO">The filtering criteria for purchases.</param>
        /// <returns>A collection of purchase DTOs matching the criteria.</returns>
        public async Task<IList<PurchaseDTO>> GetPurchaseTransactions(PurchaseRequestDTO purchaseRequestDTO)
        {
            IList<PurchaseBO> puchaseTransactions;
            try
            {
                puchaseTransactions = await purchaseRepository.GetPurchaseTransactions(purchaseRequestDTO.Ids, new CancellationToken());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to retrieve purchase transactions from repository");
                throw new PurchaseDatabaseException("Failed to retrieve purchase transactions", ex);
            }

            IList<PurchaseDTO> purchases = PurchaseMapper.MapToPurchaseDTOs(puchaseTransactions);

            return purchases;
        }
    }
}

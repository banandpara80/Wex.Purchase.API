using Serilog;
using System.ComponentModel.DataAnnotations;
using Wex.Purchase.BusinessModels;
using Wex.Purchase.Manager;
using Wex.Purchase.Common.Exceptions;

namespace Wex.Purchase.Service
{
    /// <summary>
    /// Service implementation for managing purchase operations.
    /// Coordinates purchase business logic through the purchase manager.
    /// </summary>
    public class PurchaseService : IPurchaseService
    {
        private readonly IPurchaseManager purchaseManager;
        private readonly ILogger Logger;

        /// <summary>
        /// Initializes a new instance of the PurchaseService class.
        /// </summary>
        /// <param name="purchaseManager">The purchase manager for business logic operations.</param>
        public PurchaseService(ILogger logger, IPurchaseManager purchaseManager)
        {
            this.Logger = logger;
            this.purchaseManager = purchaseManager;
        }

        /// <summary>
        /// Adds a new purchase to the system.
        /// </summary>
        /// <param name="purchaseDTO">The purchase data to add.</param>
        /// <returns>The added purchase with generated metadata.</returns>
        public async Task<PurchaseDTO> AddPurchase(PurchaseDTO purchaseDTO)
        {
            // Validate incoming DTO
            var validationContext = new ValidationContext(purchaseDTO);
            var validationResults = new List<ValidationResult>();

            if (!Validator.TryValidateObject(purchaseDTO, validationContext, validationResults, true))
            {
                var errors = validationResults.Select(r => r.ErrorMessage).Where(m => !string.IsNullOrEmpty(m)).ToList();
                throw new PurchaseValidationException("Purchase validation failed", errors);
            }

            return await purchaseManager.AddPurchase(purchaseDTO);
        }

        /// <summary>
        /// Retrieves purchase transaction based on order id.
        /// </summary>
        /// <param name="id">Purchase Id</param>
        /// <returns>Purchase DTO matching the criteria.</returns>
        public Task<PurchaseDTO> GetPurchaseOrderById(Guid id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieves purchase transactions based on specified criteria.
        /// </summary>
        /// <param name="purchaseRequestDTO">The filtering criteria for purchases.</param>
        /// <returns>A collection of purchase DTOs matching the criteria.</returns>
        public async Task<IList<PurchaseDTO>> GetPurchaseTransactions(PurchaseRequestDTO purchaseRequestDTO)
        {
            if (purchaseRequestDTO == null)
                throw new PurchaseValidationException("Purchase request cannot be null");

            // Basic request validation
            if (purchaseRequestDTO.Ids == null || purchaseRequestDTO.Ids.Length == 0)
                return new List<PurchaseDTO>();

            return await purchaseManager.GetPurchaseTransactions(purchaseRequestDTO);
        }
    }
}

using Wex.Purchase.BusinessModels;
using Wex.Purchase.Manager.EntityMapper;
using Wex.Purchase.Manager.ExchangeRateConversion;
using Wex.Purchase.Repository;
using Wex.Purchase.Repository.Entity;
using Wex.Purchase.Common.Exceptions;
using Serilog;

namespace Wex.Purchase.Manager
{
    /// <summary>
    /// Manager implementation for purchase business logic operations.
    /// Handles mapping between DTOs and entities, coordinates data operations.
    /// Supports exchange rate conversions via Treasury API integration.
    /// </summary>
    public class PurchaseManager : IPurchaseManager
    {
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IExchangeRateConversionService _exchangeRateConversionService;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the PurchaseManager class.
        /// </summary>
        /// <param name="logger">Serilog logger instance.</param>
        /// <param name="purchaseRepository">The repository for data access operations.</param>
        /// <param name="exchangeRateConversionService">Service for exchange rate conversions.</param>
        public PurchaseManager(ILogger logger, IPurchaseRepository purchaseRepository, 
            IExchangeRateConversionService exchangeRateConversionService = null) {
                _logger = logger;
                this._purchaseRepository = purchaseRepository;
                this._exchangeRateConversionService = exchangeRateConversionService;
            }

        /// <summary>
        /// Adds a new purchase to the system.
        /// </summary>
        /// <param name="purchaseDTO">The purchase data to add.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>The added purchase with generated metadata.</returns>
        public async Task<PurchaseDTO> AddPurchase(PurchaseDTO purchaseDTO, CancellationToken cancellationToken = default)
        {
            if (purchaseDTO.Id == Guid.Empty)
                purchaseDTO.Id = Guid.NewGuid();

            // Ensure PurchaseAmount is rounded to nearest cent before persisting (AwayFromZero)
            purchaseDTO.PurchaseAmount = decimal.Round(purchaseDTO.PurchaseAmount, 2, MidpointRounding.AwayFromZero);

            PurchaseBO purchaseBO = PurchaseMapper.MapToPurchaseBO(purchaseDTO);

          
            purchaseBO.ExchangeRateDate = DateOnly.FromDateTime(purchaseBO.TransactionDate);
            await _purchaseRepository.AddAsync(purchaseBO, cancellationToken);
            
            purchaseDTO = purchaseBO.MapToPurchaseDTO();

            return purchaseDTO;
        }

        public async Task<PurchaseDTO> GetPurchaseOrderById(Guid id, CancellationToken cancellationToken = default)
        {
            PurchaseBO purchaseBO = await _purchaseRepository.GetByIdAsync(id, cancellationToken);

            PurchaseDTO purchaseDTO = null;

            if(purchaseBO != null)
            {
                purchaseDTO = PurchaseMapper.MapToPurchaseDTO(purchaseBO);
            }
            return purchaseDTO; 
        }

        /// <summary>
        /// Retrieves purchase transactions based on specified criteria.
        /// </summary>
        /// <param name="purchaseRequestDTO">The filtering criteria for purchases.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A collection of purchase DTOs matching the criteria.</returns>
        public async Task<IList<PurchaseDTO>> GetPurchaseTransactions(PurchaseRequestDTO purchaseRequestDTO, CancellationToken cancellationToken = default)
        {
            // Parse the Ids string to array of Guids
            Guid[] purchaseIds;
            try
            {
                var idStrings = purchaseRequestDTO.Ids.Split(',');
                purchaseIds = idStrings
                    .Where(x => !String.IsNullOrEmpty(x))
                    .Select(id => Guid.Parse(id.Trim()))
                    .ToArray();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error parsing purchase IDs: {Ids}", purchaseRequestDTO.Ids);
                throw new ArgumentException($"Invalid purchase ID format. Expected comma-separated GUIDs.", ex);
            }

            IList<PurchaseBO> puchaseTransactions;
            try
            {
                puchaseTransactions = await _purchaseRepository.GetPurchaseTransactions(purchaseIds, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to retrieve purchase transactions from repository");
                throw new PurchaseDatabaseException("Failed to retrieve purchase transactions", ex);
            }

            IList<PurchaseDTO> purchases = PurchaseMapper.MapToPurchaseDTOs(puchaseTransactions);

            return purchases;
        }

        /// <summary>
        /// Retrieves purchase transactions with exchange rate conversions to specified currencies.
        /// Uses Treasury Reporting Rates of Exchange API for current conversion rates.
        /// </summary>
        /// <param name="purchaseRequestDTO">The filtering criteria for purchases.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A collection of purchases with exchange rate conversion information.</returns>
        public async Task<IList<PurchaseWithExchangeRateDTO>> GetPurchaseTransactionsWithConversions(PurchaseRequestDTO purchaseRequestDTO, CancellationToken cancellationToken = default)
        {
            // First, retrieve the base purchases
            IList<PurchaseDTO> purchases = await GetPurchaseTransactions(purchaseRequestDTO, cancellationToken);

            if (purchases.Count == 0)
            {
                _logger.Information("No purchases found for conversion");
                return new List<PurchaseWithExchangeRateDTO>();
            }

            // Convert to requested currencies
           
            IList<PurchaseWithExchangeRateDTO> convertedPurchases = await _exchangeRateConversionService.ConvertPurchasesAsync(
                purchases,
                purchaseRequestDTO.Currency,
                cancellationToken);

            _logger.Information("Successfully converted {PurchaseCount} purchases to {Currency} currencies",
                purchases.Count, purchaseRequestDTO.Currency);

            return convertedPurchases;
           
        }
    }
}

using Serilog;
using Wex.Purchase.BusinessModels;
using Wex.Purchase.Manager.ExchangeRate;

namespace Wex.Purchase.Manager.ExchangeRateConversion;

/// <summary>
/// Interface for exchange rate conversion service.
/// Handles conversion of purchase amounts to different currencies using Treasury API rates.
/// </summary>
public interface IExchangeRateConversionService
{
    /// <summary>
    /// Converts a purchase amount to a target currency using the exchange rate for the transaction date.
    /// </summary>
    /// <param name="purchase">The purchase to convert.</param>
    /// <param name="targetCurrencyCode">ISO 4217 target currency code.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>Enriched purchase DTO with conversion details.</returns>
    Task<PurchaseWithExchangeRateDTO> ConvertPurchaseAsync(PurchaseDTO purchase, string targetCurrencyCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts multiple purchases to target currencies in parallel.
    /// </summary>
    /// <param name="purchases">Collection of purchases to convert.</param>
    /// <param name="targetCurrencyCodes">Array of ISO 4217 target currency codes.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>Collection of enriched purchase DTOs with conversion details.</returns>
    Task<IList<PurchaseWithExchangeRateDTO>> ConvertPurchasesAsync(IList<PurchaseDTO> purchases, string[] targetCurrencyCodes, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of IExchangeRateConversionService.
/// Uses ITreasuryExchangeRateClient to fetch rates and applies conversions.
/// </summary>
public class ExchangeRateConversionService : IExchangeRateConversionService
{
    private readonly ITreasuryExchangeRateClient _exchangeRateClient;
    private readonly ILogger _logger;

    public ExchangeRateConversionService(ITreasuryExchangeRateClient exchangeRateClient, ILogger logger)
    {
        _exchangeRateClient = exchangeRateClient ?? throw new ArgumentNullException(nameof(exchangeRateClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Converts a purchase amount to a target currency using the exchange rate for the transaction date.
    /// </summary>
    public async Task<PurchaseWithExchangeRateDTO> ConvertPurchaseAsync(PurchaseDTO purchase, string targetCurrencyCode, CancellationToken cancellationToken = default)
    {
        if (purchase == null)
            throw new ArgumentNullException(nameof(purchase));

        if (string.IsNullOrWhiteSpace(targetCurrencyCode))
            throw new ArgumentException("Target currency code cannot be null or empty", nameof(targetCurrencyCode));

        _logger.Information("Converting purchase {PurchaseId} to {CurrencyCode} for date {TransactionDate}", purchase.Id, targetCurrencyCode, purchase.TransactionDate);

        try
        {
            // Use the new method with fallback to most recent rate in the past 6 months
            var exchangeRate = await _exchangeRateClient.GetExchangeRateWithFallbackAsync(targetCurrencyCode, purchase.TransactionDate, cancellationToken);

            // Convert: ConvertedAmount = PurchaseAmount / ExchangeRate
            decimal convertedAmount = purchase.PurchaseAmount / exchangeRate;

            var result = new PurchaseWithExchangeRateDTO
            {
                Purchase = purchase,
                ExchangeRate = exchangeRate,
                ConvertedAmount = Math.Round(convertedAmount, 2, MidpointRounding.AwayFromZero),
                ExchangeRateEffectiveDate = purchase.TransactionDate
            };

            _logger.Information("Purchase {PurchaseId} converted: {OriginalAmount} USD → {ConvertedAmount} {CurrencyCode} (rate: {ExchangeRate})",
                purchase.Id, purchase.PurchaseAmount, result.ConvertedAmount, targetCurrencyCode, exchangeRate);

            return result;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error converting purchase {PurchaseId} to {CurrencyCode}", purchase.Id, targetCurrencyCode);
            throw;
        }
    }

    /// <summary>
    /// Converts multiple purchases to target currencies in parallel.
    /// </summary>
    public async Task<IList<PurchaseWithExchangeRateDTO>> ConvertPurchasesAsync(IList<PurchaseDTO> purchases, string[] targetCurrencyCodes, CancellationToken cancellationToken = default)
    {
        if (purchases == null || purchases.Count == 0)
            throw new ArgumentException("Purchases list cannot be null or empty", nameof(purchases));

        if (targetCurrencyCodes == null || targetCurrencyCodes.Length == 0)
            throw new ArgumentException("Target currency codes array cannot be null or empty", nameof(targetCurrencyCodes));

        _logger.Information("Converting {PurchaseCount} purchases to {CurrencyCount} currencies", purchases.Count, targetCurrencyCodes.Length);

        try
        {
            // Create conversion tasks for all combinations
            var conversionTasks = new List<Task<PurchaseWithExchangeRateDTO>>();

            foreach (var purchase in purchases)
            {
                foreach (var currencyCode in targetCurrencyCodes)
                {
                    conversionTasks.Add(ConvertPurchaseAsync(purchase, currencyCode, cancellationToken));
                }
            }

            var results = await Task.WhenAll(conversionTasks);

            _logger.Information("Successfully converted {ResultCount} purchase-currency combinations", results.Length);
            return results.ToList();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error converting multiple purchases");
            throw;
        }
    }
}

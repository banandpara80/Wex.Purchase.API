using Serilog;
using Wex.Purchase.BusinessModels;
using Wex.Purchase.Common.Exceptions;
using Wex.Purchase.Manager.ExchangeRate;
using Wex.Purchase.Repository.Entity;

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
    Task<IList<PurchaseWithExchangeRateDTO>> ConvertPurchasesAsync(IList<PurchaseDTO> purchases, string targetCurrencyCodes, CancellationToken cancellationToken = default);
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
    /// Returns a result with a note if the exchange rate is not available instead of throwing.
    /// </summary>
    public async Task<PurchaseWithExchangeRateDTO> ConvertPurchaseAsync(PurchaseDTO purchase, string targetCurrencyCode, CancellationToken cancellationToken = default)
    {
        if (purchase == null)
            throw new ArgumentNullException(nameof(purchase));

        if (string.IsNullOrWhiteSpace(targetCurrencyCode))
            throw new ArgumentException("Target currency code cannot be null or empty", nameof(targetCurrencyCode));

        _logger.Information("Converting purchase {PurchaseId} to {CurrencyCode} for date {ExchangeRateDate}", purchase.Id, targetCurrencyCode, purchase.ExchangeRateDate);

        try
        {
            DateOnly transactionDate = DateOnly.FromDateTime(purchase.TransactionDate);
            // Use the new method with fallback to most recent rate in the past 6 months
            ExchangeRateRecord exchangeRateRecord = await _exchangeRateClient.GetExchangeRateWithFallbackAsync(targetCurrencyCode, transactionDate, cancellationToken);

            // Convert: ConvertedAmount = PurchaseAmount * ExchangeRate
            decimal convertedAmount = purchase.PurchaseAmount * exchangeRateRecord.ExchangeRate;

            var result = new PurchaseWithExchangeRateDTO
            {
                Purchase = purchase,
                ExchangeRate = exchangeRateRecord.ExchangeRate,
                ConvertedAmount = Math.Round(convertedAmount, 2, MidpointRounding.AwayFromZero),
                ExchangeRateEffectiveDate = exchangeRateRecord.RecordDate
            };

            _logger.Information("Purchase {PurchaseId} converted: {OriginalAmount} USD → {ConvertedAmount} {CurrencyCode} (rate: {ExchangeRate})",
                purchase.Id, purchase.PurchaseAmount, result.ConvertedAmount, targetCurrencyCode, exchangeRateRecord.ExchangeRate);

            return result;
        }
        catch (ExchangeRateNotFoundException ex)
        {
            _logger.Warning(ex, "Exchange rate not available for purchase {PurchaseId} to {CurrencyCode} on {ExchangeRateDate}", purchase.Id, targetCurrencyCode, purchase.ExchangeRateDate);

            // Return result with note indicating exchange rate is not available
            return new PurchaseWithExchangeRateDTO
            {
                Purchase = purchase,
                ExchangeRate = null,
                ConvertedAmount = null,
                ExchangeRateEffectiveDate = DateOnly.FromDateTime(DateTime.Now),
                Note = $"Exchange rate not available for currency '{targetCurrencyCode}' on {purchase.ExchangeRateDate:yyyy-MM-dd} or in the past 6 months."
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error converting purchase {PurchaseId} to {CurrencyCode}", purchase.Id, targetCurrencyCode);
            throw;
        }
    }

    /// <summary>
    /// Converts multiple purchases to target currencies in parallel.
    /// Returns results for all purchases, with notes for those where exchange rates are not available.
    /// </summary>
    public async Task<IList<PurchaseWithExchangeRateDTO>> ConvertPurchasesAsync(IList<PurchaseDTO> purchases, string targetCurrencyCode, CancellationToken cancellationToken = default)
    {
        if (purchases == null || purchases.Count == 0)
            throw new ArgumentException("Purchases list cannot be null or empty", nameof(purchases));

        if (String.IsNullOrEmpty(targetCurrencyCode)) 
            throw new ArgumentException("Target currency code cannot be null or empty", nameof(targetCurrencyCode));

        _logger.Information("Converting {PurchaseCount} purchases to currency {CurrencyCode}", purchases.Count, targetCurrencyCode);

        var conversionTasks = purchases.Select(purchase => ConvertPurchaseAsync(purchase, targetCurrencyCode, cancellationToken)).ToList();
        var results = await Task.WhenAll(conversionTasks);

        _logger.Information("Successfully processed {ResultCount} purchases. Available rates: {AvailableCount}, Not available: {NotAvailableCount}", 
            results.Length, 
            results.Count(r => r.ExchangeRate.HasValue),
            results.Count(r => !r.ExchangeRate.HasValue));

        return results.ToList();
    }
}

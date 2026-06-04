using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Wex.Purchase.Manager.ExchangeRate;

/// <summary>
/// Manages cached exchange rates for currencies with transaction-date-specific 6-month windows.
/// Cache key: Currency + TransactionDate, Value: Exchange rates for 6 months before transaction date.
/// Cache expires every 6 hours per key.
/// </summary>
public class ExchangeRateCacheManager
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    // Cache structure: Key = "CURRENCY_YYYY-MM-DD" (e.g., "EUR_2024-06-15"), 
    // Value = ExchangeRateCacheEntry containing rates from 6 months before that date
    private static readonly ConcurrentDictionary<string, ExchangeRateCacheEntry> ExchangeRateCache = new();

    // API endpoint for Treasury exchange rates
    private const string TreasuryApiBaseUrl = "https://api.fiscaldata.treasury.gov/services/api/fiscal_service";
    private const string ExchangeRatesEndpoint = "/v1/accounting/od/rates_of_exchange";
    private const string ExchangeRateFields = "fields=record_date,country,currency,country_currency_desc,exchange_rate";

    // Cache expiration time: 6 hours
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(6);

    public ExchangeRateCacheManager(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves exchange rates for a specific currency and transaction date.
    /// Loads rates from the 6 months preceding (and including) the transaction date.
    /// Cache key: Currency + TransactionDate.
    /// </summary>
    /// <param name="currencyCode">ISO 4217 currency code (e.g., EUR, GBP, JPY).</param>
    /// <param name="transactionDate">The transaction/purchase date - defines the 6-month window.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>List of exchange rates for the 6 months before and including the transaction date.</returns>
    public async Task<List<ExchangeRateRecord>> GetExchangeRatesForDateAsync(
        string currencyCode, 
        DateOnly transactionDate, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new ArgumentException("Currency code cannot be null or empty", nameof(currencyCode));

        // Cache key includes currency AND transaction date for date-specific 6-month windows
        string cacheKey = $"{currencyCode}_{transactionDate:yyyy-MM-dd}";

        // Check if cached and not expired
        if (ExchangeRateCache.TryGetValue(cacheKey, out var cachedEntry) && !cachedEntry.IsExpired())
        {
            _logger.Information("Retrieved exchange rates from cache: {CacheKey}", cacheKey);
            return cachedEntry.Rates ?? new List<ExchangeRateRecord>();
        }

        // Load from API for this specific date window
        await EnsureCacheLoadedAsync(currencyCode, transactionDate, cacheKey, cancellationToken);

        if (ExchangeRateCache.TryGetValue(cacheKey, out var entry))
        {
            return entry.Rates ?? new List<ExchangeRateRecord>();
        }

        throw new InvalidOperationException(
            $"No exchange rates found for currency {currencyCode} in the 6 months before {transactionDate:yyyy-MM-dd}");
    }

    /// <summary>
    /// Ensures the cache is loaded with exchange rate data for the 6-month window before the transaction date.
    /// Cache key includes transaction date to create date-specific 6-month windows.
    /// </summary>
    private async Task EnsureCacheLoadedAsync(
        string currencyCode, 
        DateOnly transactionDate, 
        string cacheKey,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Loading exchange rates for {CurrencyCode} with date window ending {TransactionDate}", 
            currencyCode, transactionDate);

        try
        {
            // Calculate date range: 6 months before transaction date (inclusive)
            DateOnly endDate = transactionDate;
            DateOnly startDate = transactionDate; transactionDate.AddMonths(-6);

            string startDateStr = startDate.ToString("yyyy-MM-dd");
            string endDateStr = endDate.ToString("yyyy-MM-dd");

            // Build API query for the 6-month period before the transaction date
            string filter = $"filter=record_date:gte:{startDateStr},record_date:lte:{endDateStr},currency:eq:{currencyCode}";
            string url = $"{TreasuryApiBaseUrl}{ExchangeRatesEndpoint}?{ExchangeRateFields}&{filter}&limit=10000";

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<TreasuryExchangeRateResponse>(
                cancellationToken: cancellationToken);

            if (apiResponse?.Data == null || apiResponse.Data.Length == 0)
            {
                _logger.Warning(
                    "No exchange rate data received from Treasury API for {CurrencyCode} in period {StartDate} to {EndDate}", 
                    currencyCode, startDateStr, endDateStr);
            }

            // Convert API records to cache format, sorted by date (most recent first)
            var rates = apiResponse.Data
                .Select(r => new ExchangeRateRecord
                {
                    Country = r.Country,
                    Currency = r.Currency,
                    CurrencyCode = r.CurrencyCode,
                    ExchangeRate = r.ExchangeRate,
                    RecordDate = ParseDate(r.EffectiveDate)
                })
                .OrderByDescending(r => r.RecordDate)
                .ToList();

            var cacheEntry = new ExchangeRateCacheEntry
            {
                Rates = rates,
                CachedAt = DateTime.UtcNow,
                TransactionDate = transactionDate
            };

            ExchangeRateCache[cacheKey] = cacheEntry;

            _logger.Information(
                "Cached {RateCount} exchange rates for {CurrencyCode} with transaction date {TransactionDate}", 
                rates.Count, currencyCode, transactionDate);
        }
        catch (HttpRequestException ex)
        {
            _logger.Error(ex, 
                "Failed to load exchange rates from Treasury API for {CurrencyCode} ending {TransactionDate}", 
                currencyCode, transactionDate);
            throw new InvalidOperationException(
                $"Failed to load exchange rates for {currencyCode}: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, 
                "Unexpected error loading exchange rates for {CurrencyCode} ending {TransactionDate}", 
                currencyCode, transactionDate);
            throw;
        }
    }

    /// <summary>
    /// Finds the exchange rate for a specific date.
    /// Rate must be less than or equal to the transaction date (rate date cannot be in the future).
    /// </summary>
    /// <param name="rates">List of exchange rates sorted by date (most recent first).</param>
    /// <param name="transactionDate">The target date - rate date must be ≤ this date.</param>
    /// <returns>The exchange rate for the exact date, or the most recent rate ≤ transaction date.
    /// Returns null if no valid rate found (all rates are after the transaction date).</returns>
    public static ExchangeRateRecord? FindExchangeRateForDateOrMostRecent(
        List<ExchangeRateRecord> rates, 
        DateOnly transactionDate)
    {
        if (rates == null || rates.Count == 0)
            return null;

        // Try to find exact date match first (compare date parts only)
        var exactMatch = rates.FirstOrDefault(r => r.RecordDate == transactionDate);
        if (exactMatch != null)
            return exactMatch;

        // Find most recent rate that is ≤ transaction date (rate cannot be in the future)
        // List is already sorted by date descending (most recent first)
        var validRate = rates.FirstOrDefault(r => r.RecordDate <= transactionDate);

        return validRate;
    }

    private static DateOnly ParseDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return DateOnly.FromDateTime(DateTime.UtcNow);

        if (DateOnly.TryParse(dateString, out var result))
            return result;

        return DateOnly.FromDateTime(DateTime.UtcNow);
    }

    /// <summary>
    /// Clears all cached exchange rates.
    /// </summary>
    public static void ClearCache()
    {
        ExchangeRateCache.Clear();
    }
}

/// <summary>
/// Represents a cached entry of exchange rates with expiration metadata.
/// </summary>
public class ExchangeRateCacheEntry
{
    /// <summary>
    /// List of exchange rates for a currency over the 6-month period before TransactionDate.
    /// </summary>
    public List<ExchangeRateRecord> Rates { get; set; } = new();

    /// <summary>
    /// The transaction date that defines the 6-month window (rates are for period before this date).
    /// </summary>
    public DateOnly TransactionDate { get; set; }

    /// <summary>
    /// Timestamp when this entry was cached.
    /// </summary>
    public DateTime CachedAt { get; set; }

    /// <summary>
    /// Checks if the cache entry has expired (older than 6 hours).
    /// </summary>
    public bool IsExpired() => DateTime.UtcNow - CachedAt > TimeSpan.FromHours(6);
}

/// <summary>
/// Represents a single exchange rate record.
/// </summary>
public class ExchangeRateRecord
{
    public string? Country { get; set; }
    public string? Currency { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal ExchangeRate { get; set; }
    public DateOnly RecordDate { get; set; }
}

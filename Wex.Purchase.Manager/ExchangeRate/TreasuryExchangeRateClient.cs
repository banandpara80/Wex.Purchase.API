using Serilog;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Wex.Purchase.Common.Exceptions;

namespace Wex.Purchase.Manager.ExchangeRate;

/// <summary>
/// Implementation of ITreasuryExchangeRateClient that calls the Treasury Reporting Rates of Exchange API.
/// Includes in-memory caching to reduce API calls for the same date/currency combination during request lifetime.
/// Supports loading 6 months of historical exchange rate data for all countries with 6-hour cache expiration.
/// API Documentation: https://fiscaldata.treasury.gov/datasets/treasury-reporting-rates-exchange/treasury-reporting-rates-of-exchange
/// </summary>
public class TreasuryExchangeRateClient : ITreasuryExchangeRateClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly ExchangeRateCacheManager _cacheManager;

    // In-memory cache: Key = "CurrencyCode|DateOnly", Value = exchange rate
    private static readonly ConcurrentDictionary<string, decimal> ExchangeRateCache = new();

    // Simple rate limiting state (sliding window)
    private readonly object _rateLimitLock = new();
    private readonly Queue<DateTime> _requestTimestamps = new();
    private readonly int _maxRequestsPerWindow;
    private readonly TimeSpan _rateLimitWindow = TimeSpan.FromMinutes(1);

    // API endpoint for Treasury exchange rates
    private const string TreasuryApiBaseUrl = "https://api.fiscaldata.treasury.gov/services/api/fiscal_service";
    private const string ExchangeRatesEndpoint = "/v1/accounting/od/rates_of_exchange";
    private const string ExchangeRateFields = "fields=record_date,country,currency,country_currency_desc,exchange_rate";

    /// <summary>
    /// Creates a new client with optional rate limit (max requests per minute).
    /// Default is 60 requests per minute.
    /// </summary>
    public TreasuryExchangeRateClient(HttpClient httpClient, ILogger logger, int maxRequestsPerMinute = 60)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheManager = new ExchangeRateCacheManager(httpClient, logger);
        _maxRequestsPerWindow = Math.Max(1, maxRequestsPerMinute);
    }

    private async Task EnsureRateLimitAsync(CancellationToken ct)
    {
        DateTime now = DateTime.UtcNow;

        while (true)
        {
            TimeSpan wait = TimeSpan.Zero;
            lock (_rateLimitLock)
            {
                // Evict timestamps outside the rolling window
                while (_requestTimestamps.Count > 0 && (now - _requestTimestamps.Peek()) >= _rateLimitWindow)
                {
                    _requestTimestamps.Dequeue();
                }

n                if (_requestTimestamps.Count < _maxRequestsPerWindow)
                {
                    _requestTimestamps.Enqueue(now);
                    return;
                }

n                var oldest = _requestTimestamps.Peek();
                wait = _rateLimitWindow - (now - oldest);
                if (wait < TimeSpan.Zero) wait = TimeSpan.Zero;
            }

            if (wait > TimeSpan.Zero)
            {
                _logger.Information("Rate limit reached. Waiting {Delay} before retrying.", wait);
                try
                {
                    await Task.Delay(wait, ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
            }

n            now = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Retrieves the exchange rate for a specific currency on a given date.
    /// Uses in-memory cache to avoid redundant API calls.
    /// </summary>
    public async Task<decimal?> GetExchangeRateAsync(string currencyCode, DateOnly transactionDate, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new ArgumentException("Currency code cannot be null or empty", nameof(currencyCode));

        string cacheKey = $"{currencyCode}|{transactionDate:yyyy-MM-dd}";

        // Check cache first
        if (ExchangeRateCache.TryGetValue(cacheKey, out var cachedRate))
        {
            _logger.Information("Exchange rate retrieved from cache: {CurrencyCode} on {Date} = {Rate}", currencyCode, transactionDate, cachedRate);
            return cachedRate;
        }

        try
        {
            _logger.Information("Fetching exchange rate from Treasury API: {CurrencyCode} on {Date}", currencyCode, transactionDate);

            // Build query with filter for specific date and currency
            string dateString = transactionDate.ToString("yyyy-MM-dd");
            string filter = $"filter=currency:eq:{currencyCode},effective_date:lt:{dateString}";
            string url = $"{TreasuryApiBaseUrl}{ExchangeRatesEndpoint}?{ExchangeRateFields}&{filter}&limit=1";

            // Ensure we respect the configured rate limit before making an external call
            await EnsureRateLimitAsync(cancellationToken);

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<TreasuryExchangeRateResponse>(cancellationToken: cancellationToken);

            if (apiResponse?.Data == null || apiResponse.Data.Length == 0)
            {
                _logger.Warning("No exchange rate found from Treasury API: {CurrencyCode} on {Date}", currencyCode, transactionDate);
                return null;
            }

            decimal rate = apiResponse.Data[0].ExchangeRate;

            // Cache the result
            ExchangeRateCache[cacheKey] = rate;
            _logger.Information("Exchange rate cached: {CurrencyCode} on {Date} = {Rate}", currencyCode, transactionDate, rate);

            return rate;
        }
        catch (HttpRequestException ex)
        {
            _logger.Error(ex, "Failed to retrieve exchange rate from Treasury API: {CurrencyCode} on {Date}", currencyCode, transactionDate);
            throw new InvalidOperationException($"Failed to retrieve exchange rate for {currencyCode} on {transactionDate}: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error retrieving exchange rate: {CurrencyCode} on {Date}", currencyCode, transactionDate);
            throw new InvalidOperationException($"Unexpected error retrieving exchange rate for {currencyCode}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Retrieves exchange rates for multiple currencies on a given date.
    /// Fetches rates in parallel and uses cache when available.
    /// </summary>
    public async Task<Dictionary<string, decimal>> GetExchangeRatesAsync(string[] currencyCodes, DateOnly transactionDate, CancellationToken cancellationToken = default)
    {
        if (currencyCodes == null || currencyCodes.Length == 0)
            throw new ArgumentException("Currency codes array cannot be null or empty", nameof(currencyCodes));

        _logger.Information("Fetching exchange rates for {Count} currencies on {Date}", currencyCodes.Length, transactionDate);

        // Fetch rates in parallel
        var tasks = currencyCodes.Distinct().Select(code => GetExchangeRateAsync(code, transactionDate, cancellationToken));
        var rates = await Task.WhenAll(tasks);

        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < currencyCodes.Distinct().Count(); i++)
        {
            string code = currencyCodes.Distinct().ElementAt(i);
            if (rates[i].HasValue)
            {
                result[code] = rates[i].Value;
            }
        }

        _logger.Information("Retrieved {Count} exchange rates on {Date}", result.Count, transactionDate);
        return result;
    }

    /// <summary>
    /// Retrieves the exchange rate for a specific currency on a given date using the 6-month cached data.
    /// Filters by currency and returns the exact date match or the most recent rate available.
    /// Cache is loaded on-demand and expires every 6 hours.
    /// </summary>
    public async Task<decimal> GetExchangeRateWithFallbackAsync(string currencyCode, DateOnly transactionDate, CancellationToken cancellationToken = default)
    {
         
        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new ArgumentException("Currency code cannot be null or empty", nameof(currencyCode));

        _logger.Information("Fetching exchange rate with fallback for {CurrencyCode} on {TransactionDate}", currencyCode, transactionDate);

        // Load 6 months of cached data for this currency and transaction date
        var exchangeRates = await _cacheManager.GetExchangeRatesForDateAsync(currencyCode, transactionDate, cancellationToken);

        // Filter to find exact date or most recent ≤ transaction date
        var exchangeRateRecord = ExchangeRateCacheManager.FindExchangeRateForDateOrMostRecent(exchangeRates, transactionDate);

        if (exchangeRateRecord == null)
        {
            _logger.Error("No exchange rate found for {CurrencyCode} on {TransactionDate} or in fallback period", currencyCode, transactionDate);
            throw new ExchangeRateNotFoundException(currencyCode, transactionDate);
        }

        if (exchangeRateRecord.RecordDate == transactionDate)
        {
            _logger.Information("Found exact exchange rate for {CurrencyCode} on {TransactionDate}: {Rate}", currencyCode, transactionDate, exchangeRateRecord.ExchangeRate);
        }
        else
        {
            _logger.Information("Using fallback exchange rate for {CurrencyCode}: requested date {TransactionDate}, using {RecordDate} with rate {Rate}",
                currencyCode, transactionDate, exchangeRateRecord.RecordDate, exchangeRateRecord.ExchangeRate);
        }

        return exchangeRateRecord.ExchangeRate;
    }
}

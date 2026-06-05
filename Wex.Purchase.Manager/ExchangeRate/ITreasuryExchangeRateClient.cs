namespace Wex.Purchase.Manager.ExchangeRate;

/// <summary>
/// Interface for Treasury Reporting Rates of Exchange API client.
/// Provides methods to retrieve exchange rates for specific currencies and dates.
/// </summary>
public interface ITreasuryExchangeRateClient
{
    /// <summary>
    /// Retrieves the exchange rate for a specific currency on a given date.
    /// </summary>
    /// <param name="currencyCode">ISO 4217 currency code (e.g., EUR, GBP, JPY).</param>
    /// <param name="exchangeRateDate">The date for which to retrieve the exchange rate.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The exchange rate as a decimal, or null if not found.</returns>
    Task<decimal?> GetExchangeRateAsync(string currencyCode, DateOnly exchangeRateDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves exchange rates for multiple currencies on a given date.
    /// </summary>
    /// <param name="currencyCodes">Array of ISO 4217 currency codes.</param>
    /// <param name="exchangeRateDate">The date for which to retrieve exchange rates.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>Dictionary mapping currency codes to exchange rates.</returns>
    Task<Dictionary<string, decimal>> GetExchangeRatesAsync(string[] currencyCodes, DateOnly exchangeRateDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the exchange rate for a specific currency on a given date using the 6-month cached data.
    /// Filters by currency and returns the exact date match or the most recent rate available.
    /// Cache is loaded on-demand and expires every 6 hours.
    /// </summary>
    /// <param name="currencyCode">ISO 4217 currency code (e.g., EUR, GBP, JPY).</param>
    /// <param name="exchangeRateDate">The date for which to retrieve the exchange rate.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The exchange rate record for the specified date, or the most recent available date.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no exchange rate is found for the currency.</exception>
    Task<ExchangeRateRecord> GetExchangeRateWithFallbackAsync(string currencyCode, DateOnly exchangeRateDate, CancellationToken cancellationToken = default);
}

namespace Wex.Purchase.Manager.ExchangeRate;

using System.Text.Json.Serialization;

/// <summary>
/// Response model for Treasury Reporting Rates of Exchange API.
/// Maps the API response structure for exchange rate data.
/// </summary>
public class TreasuryExchangeRateResponse
{
    /// <summary>
    /// Gets or sets the array of exchange rate records.
    /// </summary>
    public TreasuryExchangeRateRecord[] Data { get; set; } = Array.Empty<TreasuryExchangeRateRecord>();
}

/// <summary>
/// Individual exchange rate record from the Treasury API.
/// </summary>
public class TreasuryExchangeRateRecord
{
    /// <summary>
    /// Gets or sets the country name.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Gets or sets the currency name.
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Gets or sets the ISO 4217 currency code (e.g., EUR, GBP).
    /// </summary>
    public string? CurrencyCode { get; set; }

    /// <summary>
    /// Gets or sets the exchange rate value (units of foreign currency per U.S. dollar).
    /// Accepts both "exchange_rate" and "ExchangeRate" JSON property names.
    /// </summary>
    [JsonPropertyName("exchange_rate")]
    public decimal Exchange_Rate { get; set; }

    /// <summary>
    /// Gets or sets the effective date for this exchange rate (format: yyyy-MM-dd).
    /// </summary>
    public string? EffectiveDate { get; set; }
}

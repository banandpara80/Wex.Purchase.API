namespace Wex.Purchase.BusinessModels;

/// <summary>
/// Enriched Purchase DTO with exchange rate conversion information.
/// Extends the base PurchaseDTO with converted amounts and exchange rate metadata.
/// </summary>
public class PurchaseWithExchangeRateDTO
{
    /// <summary>
    /// Gets or sets the base purchase information.
    /// </summary>
    public required PurchaseDTO Purchase { get; set; }

    /// <summary>
    /// Gets or sets the exchange rate used for the conversion.
    /// Represents units of foreign currency per U.S. dollar.
    /// Null when exchange rate is not available.
    /// </summary>
    public decimal? ExchangeRate { get; set; }

    /// <summary>
    /// Gets or sets the converted purchase amount in the target currency.
    /// Formula: ConvertedAmount = PurchaseAmount * ExchangeRate
    /// Null when exchange rate is not available.
    /// </summary>
    public decimal? ConvertedAmount { get; set; }

    /// <summary>
    /// Gets or sets the date the exchange rate was effective (format: yyyy-MM-dd).
    /// Null when exchange rate is not available.
    /// </summary>
    public DateOnly ExchangeRateEffectiveDate { get; set; }

    /// <summary>
    /// Gets or sets a note providing additional information about the conversion.
    /// Used to indicate when exchange rate data is not available.
    /// </summary>
    public string? Note { get; set; }
}

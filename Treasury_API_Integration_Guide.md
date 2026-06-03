# Treasury Reporting Rates of Exchange API Integration

## Overview

This implementation provides a complete integration with the U.S. Treasury Department's Reporting Rates of Exchange API to retrieve and apply foreign exchange rates to purchase transactions. The system converts purchase amounts from USD to any supported currency using the official Treasury exchange rates effective on the transaction date.

**API Reference**: https://fiscaldata.treasury.gov/datasets/treasury-reporting-rates-exchange/treasury-reporting-rates-of-exchange

## Architecture

### Components

1. **ITreasuryExchangeRateClient** - Interface for Treasury API communication
2. **TreasuryExchangeRateClient** - HttpClient implementation with in-memory caching
3. **IExchangeRateConversionService** - Business logic for currency conversions
4. **ExchangeRateConversionService** - Implementation of conversion service
5. **PurchaseWithExchangeRateDTO** - Enriched DTO with conversion metadata

### Data Flow

```
Controller (API Endpoint)
	↓
IPurchaseService (GetPurchaseTransactionsWithConversions)
	↓
IPurchaseManager (GetPurchaseTransactionsWithConversions)
	↓
IExchangeRateConversionService (ConvertPurchasesAsync)
	↓
ITreasuryExchangeRateClient (GetExchangeRateAsync)
	↓
Treasury API (HTTP GET request)
```

## Usage

### 1. API Endpoint

**Endpoint**: `POST /api/v1/purchase/transactions/with-conversions`

**Request Body**:
```json
{
  "ids": [
	"550e8400-e29b-41d4-a716-446655440000",
	"550e8400-e29b-41d4-a716-446655440001"
  ],
  "currency": ["EUR", "GBP", "JPY"]
}
```

**Response**:
```json
[
  {
	"purchase": {
	  "id": "550e8400-e29b-41d4-a716-446655440000",
	  "description": "Office Supplies",
	  "purchaseAmount": 1250.50,
	  "transactionDate": "2024-01-15"
	},
	"exchangeRate": 1.0850,
	"convertedCurrencyCode": "EUR",
	"convertedAmount": 1152.53,
	"exchangeRateEffectiveDate": "2024-01-15"
  },
  {
	"purchase": {
	  "id": "550e8400-e29b-41d4-a716-446655440000",
	  "description": "Office Supplies",
	  "purchaseAmount": 1250.50,
	  "transactionDate": "2024-01-15"
	},
	"exchangeRate": 1.2685,
	"convertedCurrencyCode": "GBP",
	"convertedAmount": 985.44,
	"exchangeRateEffectiveDate": "2024-01-15"
  }
]
```

### 2. Supported Currencies

Common ISO 4217 currency codes supported by the Treasury API:
- **EUR** - Euro
- **GBP** - British Pound Sterling
- **JPY** - Japanese Yen
- **CAD** - Canadian Dollar
- **AUD** - Australian Dollar
- **CHF** - Swiss Franc
- **CNY** - Chinese Yuan Renminbi
- **INR** - Indian Rupee
- **MXN** - Mexican Peso
- **SGD** - Singapore Dollar
- **HKD** - Hong Kong Dollar
- **MYR** - Malaysian Ringgit
- **ZAR** - South African Rand
- **KRW** - South Korean Won
- **NZD** - New Zealand Dollar

*(See Treasury API documentation for complete list of supported currencies)*

## Implementation Details

### Exchange Rate Calculation

The conversion formula applies the official Treasury rate:

```
Converted Amount = Purchase Amount (USD) / Exchange Rate
```

**Example**:
- Purchase Amount: $100 USD
- Exchange Rate on transaction date: 1.10 EUR/USD
- Converted Amount: 100 / 1.10 = €90.91

**Note**: Exchange rates represent units of foreign currency per one U.S. dollar.

### Rounding Strategy

All converted amounts are rounded to 2 decimal places using `MidpointRounding.AwayFromZero`:
- 90.905 → 90.91
- 90.904 → 90.90

This ensures financial accuracy and compliance with currency standards.

### Caching Mechanism

The `TreasuryExchangeRateClient` implements in-memory caching to optimize API calls:

- **Cache Key Format**: `CURRENCYCODE|YYYY-MM-DD`
- **Scope**: Request lifetime (static ConcurrentDictionary)
- **Benefit**: Multiple currency conversions for the same date reuse cached rates

**Cache Hit Scenario**:
```
Request 1: Convert to EUR on 2024-01-15 → API Call → Rate cached
Request 2: Convert to GBP on 2024-01-15 → API Call → Rate cached
Request 3: Convert to EUR on 2024-01-15 → Cache Hit ✓ (no API call)
```

### Error Handling

The integration propagates exceptions to allow caller control:

1. **Exchange Rate Not Found**
   - HTTP 400 Bad Request
   - Currency code may not be supported for transaction date
   - Example: Holiday period or currency not actively traded

2. **API Unreachable**
   - HTTP 503 Service Unavailable
   - Treasury API is down or network issue
   - All exception details logged via Serilog

3. **Invalid Input**
   - HTTP 400 Bad Request
   - Missing purchase data, invalid currency code, or empty request

## Dependency Injection Registration

Services are automatically registered in `AddInfrastructure()`:

```csharp
// Register Treasury Exchange Rate API client and conversion service
services.AddHttpClient<ITreasuryExchangeRateClient, TreasuryExchangeRateClient>();
services.AddScoped<IExchangeRateConversionService, ExchangeRateConversionService>();
```

The `HttpClient` is registered with standard resilience handlers for retry logic and circuit breakers.

## Logging

All operations are logged using Serilog with structured logging:

```
Information: "Fetching exchange rate from Treasury API: EUR on 2024-01-15"
Information: "Exchange rate retrieved from cache: EUR on 2024-01-15 = 1.0850"
Information: "Successfully converted 5 purchase-currency combinations"
Warning: "No exchange rate found from Treasury API: XXX on 2024-01-15"
Error: "Failed to retrieve exchange rate from Treasury API: EUR on 2024-01-15"
```

## Testing

Unit tests are provided in `TreasuryExchangeRateTests.cs`:

- ✓ Correct conversion calculation
- ✓ Exchange rate lookup failures
- ✓ Batch conversions
- ✓ AwayFromZero rounding
- ✓ Null-safety validation

Run tests:
```bash
dotnet test --filter "ClassName=TreasuryExchangeRateTests"
```

## Configuration

No configuration required. The Treasury API endpoint is public and requires no authentication:
- **Base URL**: `https://api.fiscaldata.treasury.gov/services/api/fiscal_service`
- **Endpoint**: `/v1/accounting/od/rates_of_exchange`
- **Authentication**: None (public API)

## Performance Considerations

### Optimization Strategies

1. **In-Memory Caching**: Reduces API calls for repeated date/currency combinations
2. **Parallel Requests**: Multiple currency conversions execute concurrently
3. **HttpClient Reuse**: Single HttpClient with connection pooling via DI
4. **Request Cancellation**: CancellationToken support for timeout handling

### API Rate Limits

The Treasury API is public with no documented rate limits. However:
- Implement caching to minimize unnecessary requests
- Use reasonable batch sizes (< 50 currencies per request)
- Handle network timeouts gracefully

## Future Enhancements

1. **Distributed Caching**: Redis integration for multi-instance deployments
2. **Rate History**: Cache historical rates for trend analysis
3. **Currency Validation**: Pre-validate currency codes before API calls
4. **Retry Policies**: Implement exponential backoff for failed requests
5. **Metrics**: Track API latency and cache hit rates via OpenTelemetry

## Troubleshooting

### "No exchange rate found for EUR on 2024-01-15"

**Causes**:
- Currency code is not supported by Treasury API
- Transaction date is weekend/holiday (market closed)
- Date is in the future

**Solution**: 
- Verify currency code in Treasury API documentation
- Check transaction date is a business day
- Use a past date for testing

### "Failed to retrieve exchange rate from Treasury API"

**Causes**:
- Treasury API is temporarily unavailable
- Network connectivity issue
- Invalid API endpoint configuration

**Solution**:
- Check Treasury API status: https://fiscaldata.treasury.gov
- Verify network connectivity
- Review Serilog logs for detailed error message

## References

- **Treasury API Documentation**: https://fiscaldata.treasury.gov/datasets/treasury-reporting-rates-exchange/treasury-reporting-rates-of-exchange
- **ISO 4217 Currency Codes**: https://en.wikipedia.org/wiki/ISO_4217
- **Federal Reserve Exchange Rates**: https://www.federalreserve.gov/datadownload/

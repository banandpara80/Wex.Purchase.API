using Serilog;

namespace Wex.Purchase.Common.Exceptions;

/// <summary>
/// Custom exception classes for the purchase API.
/// Provides domain-specific exceptions for different error scenarios in purchase operations.
/// Base exception class for all application-specific exceptions.
/// </summary>
public class PurchaseApplicationException : Exception
{
    public PurchaseApplicationException(string message) : base(message) 
    {
        Log.Error("PurchaseApplicationException occurred: {@Message}", message);
    }

    public PurchaseApplicationException(string message, Exception innerException) 
        : base(message, innerException) 
    {
        Log.Error(innerException, "PurchaseApplicationException with inner exception: {@Message}", message);
    }
}

/// <summary>
/// Exception thrown when a purchase resource is not found.
/// </summary>
public class PurchaseNotFoundException : PurchaseApplicationException
{
    public Guid PurchaseId { get; }

    public PurchaseNotFoundException(Guid purchaseId) 
        : base($"Purchase with ID '{purchaseId}' was not found.")
    {
        PurchaseId = purchaseId;
        Log.Warning("Purchase not found - PurchaseId: {@PurchaseId}", purchaseId);
    }
}

/// <summary>
/// Exception thrown when validation fails.
/// </summary>
public class PurchaseValidationException : PurchaseApplicationException
{
    public List<string> Errors { get; }

    public PurchaseValidationException(string message, List<string> errors = null) 
        : base(message)
    {
        Errors = errors ?? new List<string>();
        Log.Warning("Validation failed - Errors: {@Errors}, Message: {@Message}", Errors, message);
    }
}

/// <summary>
/// Exception thrown when database operations fail.
/// </summary>
public class PurchaseDatabaseException : PurchaseApplicationException
{
    public PurchaseDatabaseException(string message, Exception innerException) 
        : base($"Database operation failed: {message}", innerException) 
    {
        Log.Error(innerException, "Database operation failed: {@Message}", message);
    }
}

/// <summary>
/// Exception thrown when a purchase resource is not found.
/// </summary>
public class ExchangeRateNotFoundException : PurchaseApplicationException
{
    public string Currency { get; }
    public DateOnly TransactionDate { get; }

    public ExchangeRateNotFoundException(string currency, DateOnly transactionDate)
        : base($"Exchange rate for currency '{currency}' was not found.")
    {
        Currency = currency;
        TransactionDate = transactionDate;
        Log.Warning("Exchange rate for currency  {@currency} was not found  on {@} or in the past 6 months ", currency, transactionDate);
    }
}

/// <summary>
/// Exception thrown when database operations fail.
/// </summary>
public class DuplicatePurchaseException : PurchaseApplicationException
{
    public DuplicatePurchaseException(string message, Exception innerException)
        : base(message, innerException)
    {
        Log.Error(innerException, "Database operation failed: {@Message}", message);
    }
}

using Serilog;
using System.Text.Json;
using Wex.Purchase.Common.Exceptions;
using Wex.Purchase.API.Models;

namespace Wex.Purchase.API.Middleware;

/// <summary>
/// Global exception handling middleware for the application.
/// Catches unhandled exceptions, logs them via Serilog, and returns standardized error responses.
/// Maps custom exceptions to appropriate HTTP status codes and error details.
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled exception occurred. RequestPath: {@RequestPath}, RequestMethod: {@RequestMethod}", 
                context.Request.Path, context.Request.Method);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            Title = "An error occurred"
        };

        var env = context.RequestServices.GetService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();

        switch (exception)
        {
            case PurchaseNotFoundException notFoundEx:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                response.Status = StatusCodes.Status404NotFound;
                response.Title = "Purchase Not Found";
                response.Detail = notFoundEx.Message;
                Log.Information("Purchase not found exception. PurchaseId: {@PurchaseId}", notFoundEx.PurchaseId);
                break;

            case ExchangeRateNotFoundException exChangenotFoundEx:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                response.Status = StatusCodes.Status404NotFound;
                response.Title = "Exchange Rate Not Found";
                response.Detail = exChangenotFoundEx.Message;
                Log.Information("Exchange Rate not found exception. TransactionDate: {@TransactionDate} , Currency :  {@Currency}", exChangenotFoundEx.TransactionDate, exChangenotFoundEx.Currency);
                break;

            case PurchaseValidationException validationEx:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response.Status = StatusCodes.Status400BadRequest;
                response.Title = "Validation Failed";
                response.Detail = validationEx.Message;
                response.Errors = validationEx.Errors;
                Log.Warning("Validation exception. Errors: {@Errors}", validationEx.Errors);
                break;
            case DuplicatePurchaseException validationEx:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response.Status = StatusCodes.Status400BadRequest;
                response.Title = "Duplicate Purchase";
                response.Errors = new List<string>() { validationEx.Message };
                Log.Warning("Duplicate Purchase: {@Message}", validationEx.Message);
                break;

            case PurchaseDatabaseException dbEx:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                response.Status = StatusCodes.Status500InternalServerError;
                response.Title = "Database Error";
                response.Detail = "A database error occurred while processing your request.";
                Log.Error(dbEx, "Database exception occurred");
                break;

            case PurchaseApplicationException appEx:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response.Status = StatusCodes.Status400BadRequest;
                response.Title = "Application Error";
                response.Detail = appEx.Message;
                Log.Error(appEx, "Application exception occurred");
                break;

            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                response.Status = StatusCodes.Status500InternalServerError;
                response.Title = "Internal Server Error";
                response.Detail = "An unexpected error occurred.";
                Log.Fatal(exception, "Unexpected exception occurred");
                break;
        }

        // Always include exception details in the response body for integration tests so the test can inspect the error.
        response.Detail = exception.ToString();

        var jsonResponse = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(jsonResponse);
    }
}

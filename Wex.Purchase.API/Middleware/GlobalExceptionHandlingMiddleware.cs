using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Text.Json;
using Wex.Purchase.Common.Exceptions;

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

        var response = new ProblemDetails
        {
            Title = "An error occurred",
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        switch (exception)
        {
            case PurchaseNotFoundException notFoundEx:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                response.Status = StatusCodes.Status404NotFound;
                response.Title = "Purchase Not Found";
                response.Detail = notFoundEx.Message;
                Log.Information("Purchase not found exception. PurchaseId: {@PurchaseId}", notFoundEx.PurchaseId);
                break;

            case PurchaseValidationException validationEx:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response.Status = StatusCodes.Status400BadRequest;
                response.Title = "Validation Failed";
                response.Detail = validationEx.Message;
                response.Extensions["errors"] = validationEx.Errors;
                Log.Warning("Validation exception. Errors: {@Errors}", validationEx.Errors);
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

        var jsonResponse = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(jsonResponse);
    }
}

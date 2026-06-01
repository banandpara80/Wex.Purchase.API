using Microsoft.AspNetCore.Diagnostics;
using Serilog;
using Wex.Purchase.API.Exceptions;
using Wex.Purchase.API.Middleware;

namespace Wex.Purchase.API.Extensions;

/// <summary>
/// Extension methods for configuring exception handling infrastructure.
/// Provides fluent configuration methods for registering exception handling services and middleware.
/// Integrates Serilog logging with global exception handling.
/// </summary>
public static class ExceptionHandlingExtensions
{
    /// <summary>
    /// Adds global exception handling middleware to the application pipeline.
    /// </summary>
    /// <param name="app">The web application builder.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication UseGlobalExceptionHandling(this WebApplication app)
    {
        Log.Information("Registering global exception handling middleware");
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        return app;
    }

    /// <summary>
    /// Adds exception handling services to the dependency injection container with Serilog.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
    {
        Log.Information("Configuring exception handling services");
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        return services;
    }
}

/// <summary>
/// Global exception handler for processing exceptions before returning to client with Serilog logging.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        try
        {
            Log.Error(exception, "Exception handled by GlobalExceptionHandler: {@ExceptionMessage}", exception.Message);

            var response = new
            {
                message = exception.Message,
                statusCode = httpContext.Response.StatusCode,
                timestamp = DateTime.UtcNow
            };

            httpContext.Response.ContentType = "application/json";

            if (exception is PurchaseNotFoundException)
            {
                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                Log.Information("Handled PurchaseNotFoundException");
            }
            else if (exception is PurchaseValidationException validationEx)
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                Log.Warning("Handled PurchaseValidationException with errors: {@Errors}", validationEx.Errors);
            }
            else if (exception is PurchaseDatabaseException)
            {
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                Log.Error("Handled PurchaseDatabaseException");
            }
            else
            {
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                Log.Fatal("Handled unexpected exception type");
            }

            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Error in GlobalExceptionHandler while handling exception");
            return false;
        }
    }
}

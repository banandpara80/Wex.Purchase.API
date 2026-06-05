using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Wex.Purchase.Common.CircuitBreaker;
using Polly.CircuitBreaker;
using Serilog;
using Wex.Purchase.API.Middleware;
using Wex.Purchase.API.Models;
using Wex.Purchase.Common.Exceptions;
using Wex.Purchase.Manager;
using Wex.Purchase.Manager.Exceptions;
using Wex.Purchase.Repository;
using Wex.Purchase.Repository.Exceptions;
using Wex.Purchase.Service;
using Wex.Purchase.Service.Exceptions;

namespace Wex.Purchase.API.Extensions
{
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
        public static IServiceCollection AddGlobalExceptionHandling(this IServiceCollection services)
        {
            Log.Information("Configuring exception handling services");
            services.AddExceptionHandler<GlobalExceptionHandler>();
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

                var response = new ErrorResponse
                {
                    Title = "An error occurred"
                };

                httpContext.Response.ContentType = "application/json";

                if (exception is PurchaseNotFoundException notFoundEx)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                    response.Status = StatusCodes.Status404NotFound;
                    response.Title = "Purchase Not Found";
                    response.Detail = notFoundEx.Message;
                    Log.Information("Handled PurchaseNotFoundException");
                }
                else if (exception is PurchaseValidationException validationEx)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    response.Status = StatusCodes.Status400BadRequest;
                    response.Title = "Validation Failed";
                    response.Detail = validationEx.Message;
                    response.Errors = validationEx.Errors;
                    Log.Warning("Handled PurchaseValidationException with errors: {@Errors}", validationEx.Errors);
                }
                else if (exception is PurchaseDatabaseException dbEx)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    response.Status = StatusCodes.Status500InternalServerError;
                    response.Title = "Database Error";
                    response.Detail = "A database error occurred while processing your request.";
                    Log.Error(dbEx, "Handled PurchaseDatabaseException");
                }
                else if (exception is ExchangeRateNotFoundException exRateEx)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                    response.Status = StatusCodes.Status404NotFound;
                    response.Title = "Exchange Rate Not Found";
                    response.Detail = exRateEx.Message;
                    Log.Error(exRateEx, "Handled ExchangeRateNotFoundException");
                }
                else if (exception is PurchaseApplicationException appEx)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    response.Status = StatusCodes.Status400BadRequest;
                    response.Title = "Application Error";
                    response.Detail = appEx.Message;
                    Log.Error(appEx, "Handled PurchaseApplicationException");
                }
                else
                {
                    httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    response.Status = StatusCodes.Status500InternalServerError;
                    response.Title = "Internal Server Error";
                    response.Detail = "An unexpected error occurred.";
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

}

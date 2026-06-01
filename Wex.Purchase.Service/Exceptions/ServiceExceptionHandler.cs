using Serilog;

namespace Wex.Purchase.Service.Exceptions;

/// <summary>
/// Exception handler utility for service layer operations.
/// Provides centralized exception handling with Serilog logging for business logic errors.
/// Wraps service operations with try-catch logic to capture and log exceptions at the business logic layer.
/// </summary>
public static class ServiceExceptionHandler
{
    public delegate Task<T> ServiceOperation<T>();

    /// <summary>
    /// Executes a service operation with exception handling and Serilog logging.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The service operation to execute.</param>
    /// <param name="operationName">The name of the operation for logging purposes.</param>
    /// <returns>The result of the operation or throws a wrapped exception.</returns>
    public static async Task<T> ExecuteWithExceptionHandling<T>(
        ServiceOperation<T> operation,
        string operationName)
    {
        try
        {
            Log.Information("Service operation starting: {@OperationName}", operationName);
            var result = await operation();
            Log.Information("Service operation completed successfully: {@OperationName}", operationName);
            return result;
        }
        catch (ArgumentNullException ex)
        {
            Log.Error(ex, "Null argument in service operation {@OperationName}, Parameter: {@ParamName}", operationName, ex.ParamName);
            throw;
        }
        catch (ArgumentException ex)
        {
            Log.Error(ex, "Invalid argument in service operation {@OperationName}: {@Message}", operationName, ex.Message);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            Log.Error(ex, "Invalid operation in service {@OperationName}: {@Message}", operationName, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error in service operation {@OperationName}: {@Message}", operationName, ex.Message);
            throw new InvalidOperationException($"Service operation '{operationName}' failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Executes a void service operation with exception handling and Serilog logging.
    /// </summary>
    /// <param name="operation">The service operation to execute.</param>
    /// <param name="operationName">The name of the operation for logging purposes.</param>
    public static async Task ExecuteWithExceptionHandling(
        Func<Task> operation,
        string operationName)
    {
        try
        {
            Log.Information("Service operation (void) starting: {@OperationName}", operationName);
            await operation();
            Log.Information("Service operation (void) completed successfully: {@OperationName}", operationName);
        }
        catch (ArgumentNullException ex)
        {
            Log.Error(ex, "Null argument in void service operation {@OperationName}, Parameter: {@ParamName}", operationName, ex.ParamName);
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in void service operation {@OperationName}: {@Message}", operationName, ex.Message);
            throw;
        }
    }
}

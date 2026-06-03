using Serilog;

namespace Wex.Purchase.Service.Exceptions;

/// <summary>
/// Exception handler utility for service layer operations.
/// Provides centralized exception handling with Serilog logging for business logic errors.
/// Handles both regular exceptions and OperationCanceledException for proper cancellation token support.
/// This implementation is injectable so services can depend on the handler via DI.
/// </summary>
public class ServiceExceptionHandler : IServiceExceptionHandler
{
    ILogger logger;
    public ServiceExceptionHandler(ILogger logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Executes a service operation with exception handling, including cancellation support.
    /// </summary>
    public async Task<T> ExecuteWithExceptionHandling<T>(Func<CancellationToken, Task<T>> operation, string operationName, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.Information("Service operation starting: {@OperationName}", operationName);
            var result = await operation(cancellationToken);
            logger.Information("Service operation completed successfully: {@OperationName}", operationName);
            return result;
        }
        catch (OperationCanceledException ex)
        {
            logger.Warning(ex, "Service operation cancelled: {@OperationName}", operationName);
            throw;
        }
        catch (ArgumentNullException ex)
        {
            logger.Error(ex, "Null argument in service operation {@OperationName}, Parameter: {@ParamName}", operationName, ex.ParamName);
            throw;
        }
        catch (ArgumentException ex)
        {
            logger.Error(ex, "Invalid argument in service operation {@OperationName}: {@Message}", operationName, ex.Message);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            logger.Error(ex, "Invalid operation in service {@OperationName}: {@Message}", operationName, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unexpected error in service operation {@OperationName}: {@Message}", operationName, ex.Message);
            throw new InvalidOperationException($"Service operation '{operationName}' failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Executes a void service operation with exception handling and cancellation support.
    /// </summary>
    public async Task ExecuteWithExceptionHandling(Func<CancellationToken, Task> operation, string operationName, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.Information("Service operation (void) starting: {@OperationName}", operationName);
            await operation(cancellationToken);
            logger.Information("Service operation (void) completed successfully: {@OperationName}", operationName);
        }
        catch (OperationCanceledException ex)
        {
            logger.Warning(ex, "Service operation (void) cancelled: {@OperationName}", operationName);
            throw;
        }
        catch (ArgumentNullException ex)
        {
            logger.Error(ex, "Null argument in void service operation {@OperationName}, Parameter: {@ParamName}", operationName, ex.ParamName);
            throw;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error in void service operation {@OperationName}: {@Message}", operationName, ex.Message);
            throw;
        }
    }
}

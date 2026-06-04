using System;
using Serilog;
using Wex.Purchase.Common.Exceptions;

namespace Wex.Purchase.Manager.Exceptions
{
    public class ManagerExceptionHandler : IManagerExceptionHandler
    {
        ILogger logger;
        public ManagerExceptionHandler(ILogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Executes a manager operation with exception handling, including cancellation support.
        /// </summary>
        public async Task<T> ExecuteWithExceptionHandling<T>(Func<CancellationToken, Task<T>> operation, string operationName, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.Information("Manager operation starting: {@OperationName}", operationName);
                var result = await operation(cancellationToken);
                logger.Information("Manager operation completed successfully: {@OperationName}", operationName);
                return result;
            }
            catch (OperationCanceledException ex)
            {
                logger.Warning(ex, "Manager operation cancelled: {@OperationName}", operationName);
                throw;
            }
            catch (ArgumentNullException ex)
            {
                logger.Error(ex, "Null argument in manager operation {@OperationName}, Parameter: {@ParamName}", operationName, ex.ParamName);
                throw;
            }
            catch (ArgumentException ex)
            {
                logger.Error(ex, "Invalid argument in manager operation {@OperationName}: {@Message}", operationName, ex.Message);
                throw;
            }
            catch (ExchangeRateNotFoundException ex)
            {
                logger.Error(ex, "Invalid operation in manager {@OperationName}: {@Message}", operationName, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Unexpected error in manager operation {@OperationName}: {@Message}", operationName, ex.Message);
                throw new Exception($"manager operation '{operationName}' failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Executes a void manager operation with exception handling and cancellation support.
        /// </summary>
        public async Task ExecuteWithExceptionHandling(Func<CancellationToken, Task> operation, string operationName, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.Information("Manager operation (void) starting: {@OperationName}", operationName);
                await operation(cancellationToken);
                logger.Information("Manager operation (void) completed successfully: {@OperationName}", operationName);
            }
            catch (OperationCanceledException ex)
            {
                logger.Warning(ex, "Manager operation (void) cancelled: {@OperationName}", operationName);
                throw;
            }
            catch (ArgumentNullException ex)
            {
                logger.Error(ex, "Null argument in void manager operation {@OperationName}, Parameter: {@ParamName}", operationName, ex.ParamName);
                throw;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in void service manager {@OperationName}: {@Message}", operationName, ex.Message);
                throw;
            }
        }
    }
}

using Serilog;

namespace Wex.Purchase.Repository.Exceptions;

/// <summary>
/// Exception handler utility for repository layer operations.
/// Provides centralized exception handling with Serilog logging for database and entity framework operations.
/// Wraps repository operations with try-catch logic to capture and log exceptions at the data access layer.
/// </summary>
public static class RepositoryExceptionHandler
{
    public delegate Task<T> RepositoryOperation<T>();

    /// <summary>
    /// Executes a repository operation with exception handling and Serilog logging.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The repository operation to execute.</param>
    /// <param name="operationName">The name of the operation for logging purposes.</param>
    /// <returns>The result of the operation or throws a wrapped exception.</returns>
    public static async Task<T> ExecuteWithExceptionHandling<T>(
        RepositoryOperation<T> operation,
        string operationName)
    {
        try
        {
            Log.Information("Repository operation starting: {@OperationName}", operationName);
            var result = await operation();
            Log.Information("Repository operation completed successfully: {@OperationName}", operationName);
            return result;
        }
        catch (InvalidOperationException ex)
        {
            Log.Error(ex, "Invalid operation in repository {@OperationName}: {@Message}", operationName, ex.Message);
            throw new InvalidOperationException($"Repository operation '{operationName}' failed: {ex.Message}", ex);
        }
        catch (ArgumentNullException ex)
        {
            Log.Error(ex, "Null argument in repository operation {@OperationName}, Parameter: {@ParamName}", operationName, ex.ParamName);
            throw new ArgumentNullException(ex.ParamName, $"Repository operation '{operationName}' received null argument: {ex.Message}");
        }
        catch (System.Data.Common.DbException dbEx)
        {
            Log.Error(dbEx, "Database exception in repository operation {@OperationName}: {@Message}", operationName, dbEx.Message);
            throw new InvalidOperationException($"Database error in repository operation '{operationName}': {dbEx.Message}", dbEx);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error in repository operation {@OperationName}: {@Message}", operationName, ex.Message);
            throw new InvalidOperationException($"Repository operation '{operationName}' failed unexpectedly: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Executes a void repository operation (Task) with exception handling and Serilog logging.
    /// </summary>
    /// <param name="operation">The repository operation to execute.</param>
    /// <param name="operationName">The name of the operation for logging purposes.</param>
    public static async Task ExecuteWithExceptionHandling(Func<Task> operation, string operationName)
    {
        try
        {
            Log.Information("Repository operation starting: {@OperationName}", operationName);
            await operation();
            Log.Information("Repository operation completed successfully: {@OperationName}", operationName);
        }
        catch (InvalidOperationException ex)
        {
            Log.Error(ex, "Invalid operation in repository {@OperationName}: {@Message}", operationName, ex.Message);
            throw new InvalidOperationException($"Repository operation '{operationName}' failed: {ex.Message}", ex);
        }
        catch (ArgumentNullException ex)
        {
            Log.Error(ex, "Null argument in repository operation {@OperationName}, Parameter: {@ParamName}", operationName, ex.ParamName);
            throw new ArgumentNullException(ex.ParamName, $"Repository operation '{operationName}' received null argument: {ex.Message}");
        }
        catch (System.Data.Common.DbException dbEx)
        {
            Log.Error(dbEx, "Database exception in repository operation {@OperationName}: {@Message}", operationName, dbEx.Message);
            throw new InvalidOperationException($"Database error in repository operation '{operationName}': {dbEx.Message}", dbEx);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error in repository operation {@OperationName}: {@Message}", operationName, ex.Message);
            throw new InvalidOperationException($"Repository operation '{operationName}' failed unexpectedly: {ex.Message}", ex);
        }
    }
}

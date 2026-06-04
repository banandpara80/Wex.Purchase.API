using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wex.Purchase.Repository.Exceptions
{
    public interface IRepositoryrExceptionHandler
    {
        /// <summary>
        /// Executes an async operation with exception handling, including OperationCanceledException.
        /// </summary>
        /// <typeparam name="T">Return type of the operation.</typeparam>
        /// <param name="operation">The async operation to execute.</param>
        /// <param name="operationName">Name of the operation for logging.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>The result of the operation.</returns>
        Task<T> ExecuteWithExceptionHandling<T>(Func<CancellationToken, Task<T>> operation, string operationName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a void async operation with exception handling, including OperationCanceledException.
        /// </summary>
        /// <param name="operation">The async operation to execute.</param>
        /// <param name="operationName">Name of the operation for logging.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A task representing the operation.</returns>
        Task ExecuteWithExceptionHandling(Func<CancellationToken, Task> operation, string operationName, CancellationToken cancellationToken = default);
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Wex.Purchase.Common.CircuitBreaker
{
    /// <summary>
    /// A no-op circuit breaker used as a default implementation when circuit breaking is not configured.
    /// It simply executes the provided action immediately.
    /// </summary>
    public class NoopCircuitBreaker : ICircuitBreaker
    {
        public bool IsOpen => false;

        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return action(cancellationToken);
        }

        public Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return action(cancellationToken);
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Wex.Purchase.Common.CircuitBreaker
{
    public interface ICircuitBreaker
    {
        Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken);
        Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken);
        bool IsOpen { get; }
    }
}

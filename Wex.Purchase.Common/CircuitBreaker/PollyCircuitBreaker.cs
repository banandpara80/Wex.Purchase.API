using System;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.CircuitBreaker;

namespace Wex.Purchase.Common.CircuitBreaker
{
    public class PollyCircuitBreaker : ICircuitBreaker
    {
        private readonly AsyncCircuitBreakerPolicy _policy;

        public PollyCircuitBreaker(int exceptionsAllowedBeforeBreaking = 2, TimeSpan? durationOfBreak = null)
        {
            durationOfBreak ??= TimeSpan.FromSeconds(30);
            _policy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(exceptionsAllowedBeforeBreaking, durationOfBreak.Value);
        }

        public bool IsOpen => _policy.CircuitState == CircuitState.Open;

        public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
        {

            try
            {
                return await _policy.ExecuteAsync(ct => action(ct), cancellationToken);
            }
            catch
            {
                throw;
            }
        }

        public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            await _policy.ExecuteAsync(ct => action(ct), cancellationToken);
        }
    }
}

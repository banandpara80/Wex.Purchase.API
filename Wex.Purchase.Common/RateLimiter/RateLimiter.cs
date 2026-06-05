using Polly.RateLimit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace Wex.Purchase.Common.RateLimiter
{
    /// <summary>
    /// Rate limiter that enforces a sliding window rate limit policy.
    /// Uses configurable request limits and time windows for thread-safe request throttling.
    /// </summary>
    public class RateLimiter : IRateLimiter
    {
        private readonly ILogger _logger;
        private readonly int _maxRequestsPerWindow;
        private readonly TimeSpan _rateLimitWindow;

        private static readonly object _rateLimitLock = new();
        private static readonly Queue<DateTime> _requestTimestamps = new();

        /// <summary>
        /// Initializes a new instance of the RateLimiter class.
        /// </summary>
        /// <param name="logger">Logger instance for logging rate limit events.</param>
        /// <param name="maxRequestsPerWindow">Maximum number of requests allowed in the time window. Default: 60.</param>
        /// <param name="windowSizeInSeconds">Size of the time window in seconds. Default: 60.</param>
        public RateLimiter(ILogger logger, int maxRequestsPerWindow = 60, int windowSizeInSeconds = 60)
        {
            _logger = logger;
            _maxRequestsPerWindow = maxRequestsPerWindow;
            _rateLimitWindow = TimeSpan.FromSeconds(windowSizeInSeconds);
        }

        /// <summary>
        /// Ensures the current request complies with the rate limit policy.
        /// Delays the request if necessary to stay within configured limits.
        /// </summary>
        /// <param name="ct">Cancellation token to support request cancellation.</param>
        public async Task EnsureRateLimitAsync(CancellationToken ct)
        {
            DateTime now = DateTime.UtcNow;
            while (true)
            {
                TimeSpan wait = TimeSpan.Zero;
                lock (_rateLimitLock)
                {
                    // Remove timestamps outside the current window
                    while (_requestTimestamps.Count > 0 && (now - _requestTimestamps.Peek()) >= _rateLimitWindow)
                    {
                        _requestTimestamps.Dequeue();
                    }

                    // If we have capacity, accept the request immediately
                    if (_requestTimestamps.Count < _maxRequestsPerWindow)
                    {
                        _requestTimestamps.Enqueue(now);
                        return;
                    }

                    // Calculate how long to wait before retrying
                    var oldest = _requestTimestamps.Peek();
                    wait = _rateLimitWindow - (now - oldest);
                    if (wait < TimeSpan.Zero) wait = TimeSpan.Zero;
                }

                if (wait > TimeSpan.Zero)
                {
                    _logger.Information("Rate limit reached ({MaxRequests} requests per {WindowSeconds} seconds). Waiting {Delay} before retrying.",
                        _maxRequestsPerWindow, _rateLimitWindow.TotalSeconds, wait);
                    try
                    {
                        await Task.Delay(wait, ct);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                }

                now = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Clears the internal rate limit state. Useful for testing.
        /// </summary>
        internal static void ClearState()
        {
            lock (_rateLimitLock)
            {
                _requestTimestamps.Clear();
            }
        }
    }
}

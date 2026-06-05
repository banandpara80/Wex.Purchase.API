using Polly.RateLimit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace Wex.Purchase.Common.RateLimiter
{
    public class RateLimiter : IRateLimiter
    {
        private readonly ILogger _logger;

        private static readonly object _rateLimitLock = new();
        private static readonly Queue<DateTime> _requestTimestamps = new();
        private const int _maxRequestsPerWindow = 60; // requests per minute
        private static readonly TimeSpan _rateLimitWindow = TimeSpan.FromMinutes(1);

        public RateLimiter(ILogger logger)
        {
            _logger = logger;
        }

        public async Task EnsureRateLimitAsync(CancellationToken ct)
        {
            DateTime now = DateTime.UtcNow;
            while (true)
            {
                TimeSpan wait = TimeSpan.Zero;
                lock (_rateLimitLock)
                {
                    while (_requestTimestamps.Count > 0 && (now - _requestTimestamps.Peek()) >= _rateLimitWindow)
                    {
                        _requestTimestamps.Dequeue();
                    }

                    if (_requestTimestamps.Count < _maxRequestsPerWindow)
                    {
                        _requestTimestamps.Enqueue(now);
                        return;
                    }

                    var oldest = _requestTimestamps.Peek();
                    wait = _rateLimitWindow - (now - oldest);
                    if (wait < TimeSpan.Zero) wait = TimeSpan.Zero;
                }

                if (wait > TimeSpan.Zero)
                {
                    _logger.Information("Rate limit reached. Waiting {Delay} before retrying.", wait);
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
    }
}

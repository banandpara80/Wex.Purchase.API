using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wex.Purchase.Common.RateLimiter
{
    public interface IRateLimiter
    {
        Task EnsureRateLimitAsync(CancellationToken ct);
    }
}

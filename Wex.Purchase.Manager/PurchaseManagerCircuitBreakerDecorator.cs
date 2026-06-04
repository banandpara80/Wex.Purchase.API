using System;
using System.Threading;
using System.Threading.Tasks;
using Wex.Purchase.BusinessModels;
using Wex.Purchase.Common.CircuitBreaker;

namespace Wex.Purchase.Manager
{
    public class PurchaseManagerCircuitBreakerDecorator : IPurchaseManager
    {
        private readonly IPurchaseManager _inner; // underlying manager (could be real PurchaseManager)
        private readonly ICircuitBreaker _circuitBreaker;

        public PurchaseManagerCircuitBreakerDecorator(IPurchaseManager inner, ICircuitBreaker circuitBreaker)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _circuitBreaker = circuitBreaker ?? throw new ArgumentNullException(nameof(circuitBreaker));
        }

        public Task<PurchaseDTO> AddPurchase(PurchaseDTO purchase, CancellationToken cancellationToken)
        {
            return _circuitBreaker.ExecuteAsync(ct => _inner.AddPurchase(purchase, ct), cancellationToken);
        }

        public Task<PurchaseDTO> GetPurchaseOrderById(Guid id, CancellationToken cancellationToken)
        {
            return _circuitBreaker.ExecuteAsync(ct => _inner.GetPurchaseOrderById(id, ct), cancellationToken);
        }

        public Task<IList<PurchaseDTO>> GetPurchaseTransactions(PurchaseRequestDTO request, CancellationToken cancellationToken)
        {
            return _circuitBreaker.ExecuteAsync(ct => _inner.GetPurchaseTransactions(request, ct), cancellationToken);
        }

        public Task<IList<PurchaseWithExchangeRateDTO>> GetPurchaseTransactionsWithConversions(PurchaseRequestDTO request, CancellationToken cancellationToken)
        {
            return _circuitBreaker.ExecuteAsync(ct => _inner.GetPurchaseTransactionsWithConversions(request, ct), cancellationToken);
        }
    }
}

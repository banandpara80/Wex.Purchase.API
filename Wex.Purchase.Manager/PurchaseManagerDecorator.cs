using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Wex.Purchase.BusinessModels;
using Wex.Purchase.Manager.Exceptions;

namespace Wex.Purchase.Manager
{
    // Decorator mirrors IPurchaseManager and delegates to real PurchaseManager
    public class PurchaseManagerDecorator : IPurchaseManager
    {
        private readonly PurchaseManager _purchaseManager;
        private readonly IManagerExceptionHandler _handler;

        public PurchaseManagerDecorator(PurchaseManager purchaseManager, IManagerExceptionHandler handler)
        {
            _purchaseManager = purchaseManager;
            _handler = handler;
        }

        public Task<PurchaseDTO> AddPurchase(PurchaseDTO purchaseDTO, CancellationToken cancellationToken = default)
        {
            return _handler.ExecuteWithExceptionHandling(
                ct => _purchaseManager.AddPurchase(purchaseDTO, ct),
                nameof(AddPurchase),
                cancellationToken);
        }

        public Task<PurchaseDTO> GetPurchaseOrderById(Guid id, CancellationToken cancellationToken = default)
        {
            return _handler.ExecuteWithExceptionHandling(
                ct => _purchaseManager.GetPurchaseOrderById(id, ct),
                nameof(GetPurchaseOrderById),
                cancellationToken);
        }

        public Task<IList<PurchaseDTO>> GetPurchaseTransactions(PurchaseRequestDTO purchaseRequestDTO, CancellationToken cancellationToken = default)
        {
            return _handler.ExecuteWithExceptionHandling(
             ct => _purchaseManager.GetPurchaseTransactions(purchaseRequestDTO, ct),
             nameof(GetPurchaseTransactions),
             cancellationToken);
        }

        public Task<IList<PurchaseWithExchangeRateDTO>> GetPurchaseTransactionsWithConversions(PurchaseRequestDTO purchaseRequestDTO, CancellationToken cancellationToken = default)
        {
            return _handler.ExecuteWithExceptionHandling(
                ct => _purchaseManager.GetPurchaseTransactionsWithConversions(purchaseRequestDTO, ct),
                nameof(GetPurchaseTransactionsWithConversions),
                cancellationToken);
        }
    }
}

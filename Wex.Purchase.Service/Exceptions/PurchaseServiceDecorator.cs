using Wex.Purchase.BusinessModels;

namespace Wex.Purchase.Service.Exceptions;

/// <summary>
/// Decorator for IPurchaseService that wraps all calls in IServiceExceptionHandler.
/// Applies exception handling globally and propagates CancellationToken through the call chain.
/// Register this in DI to apply exception handling without modifying each method.
/// </summary>
public class PurchaseServiceDecorator : IPurchaseService
{
    private readonly IPurchaseService _inner;
    private readonly IServiceExceptionHandler _handler;

    public PurchaseServiceDecorator(IPurchaseService inner, IServiceExceptionHandler handler)
    {
        _inner = inner;
        _handler = handler;
    }

    public Task<PurchaseDTO> AddPurchase(PurchaseDTO purchaseDTO, CancellationToken cancellationToken = default)
    {
        return _handler.ExecuteWithExceptionHandling(
            ct => _inner.AddPurchase(purchaseDTO, ct),
            nameof(AddPurchase),
            cancellationToken);
    }

    public Task<PurchaseDTO> GetPurchaseOrderById(Guid id, CancellationToken cancellationToken = default)
    {
        return _handler.ExecuteWithExceptionHandling(
            ct => _inner.GetPurchaseOrderById(id, ct),
            nameof(GetPurchaseOrderById),
            cancellationToken);
    }

    public Task<IList<PurchaseWithExchangeRateDTO>> GetPurchaseTransactionsWithConversions(PurchaseRequestDTO purchaseRequestDTO, CancellationToken cancellationToken = default)
    {
        return _handler.ExecuteWithExceptionHandling(
            ct => _inner.GetPurchaseTransactionsWithConversions(purchaseRequestDTO, ct),
            nameof(GetPurchaseTransactionsWithConversions),
            cancellationToken);
    }
}

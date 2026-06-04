using Serilog;
using Wex.Purchase.Repository.Entity;
using System.Diagnostics;

namespace Wex.Purchase.Repository.Exceptions;

public class PurchaseRepositoryDecorator : IPurchaseRepository
{
    private readonly IPurchaseRepository _inner;
    private readonly IRepositoryrExceptionHandler _handler;

    public PurchaseRepositoryDecorator(IPurchaseRepository inner, IRepositoryrExceptionHandler handler)
    {
        _inner = inner;
        _handler= handler;
    }

    public Task AddAsync(PurchaseBO purchase, CancellationToken cancellationToken = default)
    {

        return _handler.ExecuteWithExceptionHandling(
            ct => _inner.AddAsync(purchase, ct),
            nameof(AddAsync),
            cancellationToken);
    }

    public Task<IEnumerable<PurchaseBO>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _handler.ExecuteWithExceptionHandling(
            ct => _inner.GetAllAsync(ct),
            nameof(GetAllAsync),
            cancellationToken);
    }

    public Task<PurchaseBO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _handler.ExecuteWithExceptionHandling(
           ct => _inner.GetByIdAsync(id, ct),
           nameof(GetByIdAsync),
           cancellationToken);
    }

    public Task<IList<PurchaseBO>> GetPurchaseTransactions(Guid[] ids, CancellationToken cancellationToken = default)
    {
        return _handler.ExecuteWithExceptionHandling(
          ct => _inner.GetPurchaseTransactions(ids, ct),
          nameof(GetPurchaseTransactions),
          cancellationToken);
    }
}

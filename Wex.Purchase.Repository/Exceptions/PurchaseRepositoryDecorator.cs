using Serilog;
using Wex.Purchase.Repository.Entity;
using System.Diagnostics;

namespace Wex.Purchase.Repository.Exceptions;

public class PurchaseRepositoryDecorator : IPurchaseRepository
{
    private readonly IPurchaseRepository _inner;
    private static readonly ActivitySource ActivitySource = new("Wex.Purchase.Repository.PurchaseRepositoryDecorator");

    public PurchaseRepositoryDecorator(IPurchaseRepository inner)
    {
        _inner = inner;
    }

    public Task AddAsync(PurchaseBO purchase, CancellationToken cancellationToken = default)
    {
        return ExecuteWithMetrics(() => RepositoryExceptionHandler.ExecuteWithExceptionHandling(() => _inner.AddAsync(purchase, cancellationToken), nameof(AddAsync)), nameof(AddAsync));
    }

    public Task<IEnumerable<PurchaseBO>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return ExecuteWithMetrics(() => RepositoryExceptionHandler.ExecuteWithExceptionHandling(() => _inner.GetAllAsync(cancellationToken), nameof(GetAllAsync)), nameof(GetAllAsync));
    }

    public Task<PurchaseBO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return ExecuteWithMetrics(() => RepositoryExceptionHandler.ExecuteWithExceptionHandling(() => _inner.GetByIdAsync(id, cancellationToken), nameof(GetByIdAsync)), nameof(GetByIdAsync));
    }

    public Task<IList<PurchaseBO>> GetPurchaseTransactions(Guid[] ids, CancellationToken cancellationToken = default)
    {
        return ExecuteWithMetrics(() => RepositoryExceptionHandler.ExecuteWithExceptionHandling(() => _inner.GetPurchaseTransactions(ids, cancellationToken), nameof(GetPurchaseTransactions)), nameof(GetPurchaseTransactions));
    }

    private async Task<T> ExecuteWithMetrics<T>(Func<Task<T>> operation, string operationName)
    {
        var activity = ActivitySource.StartActivity(operationName, ActivityKind.Internal);
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await operation().ConfigureAwait(false);
            Log.Information("Repository operation {Operation} completed in {ElapsedMs}ms", operationName, sw.ElapsedMilliseconds);
            activity?.SetTag("otel.status_code", "OK");
            activity?.SetTag("duration_ms", sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            activity?.SetTag("otel.status_code", "ERROR");
            activity?.SetTag("otel.error", true);
            activity?.SetTag("error.message", ex.Message);
            Log.Error(ex, "Repository operation {Operation} failed after {ElapsedMs}ms", operationName, sw.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            sw.Stop();
            activity?.Stop();
        }
    }

    private async Task ExecuteWithMetrics(Func<Task> operation, string operationName)
    {
        var activity = ActivitySource.StartActivity(operationName, ActivityKind.Internal);
        var sw = Stopwatch.StartNew();
        try
        {
            await operation().ConfigureAwait(false);
            Log.Information("Repository operation {Operation} completed in {ElapsedMs}ms", operationName, sw.ElapsedMilliseconds);
            activity?.SetTag("otel.status_code", "OK");
            activity?.SetTag("duration_ms", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            activity?.SetTag("otel.status_code", "ERROR");
            activity?.SetTag("otel.error", true);
            activity?.SetTag("error.message", ex.Message);
            Log.Error(ex, "Repository operation {Operation} failed after {ElapsedMs}ms", operationName, sw.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            sw.Stop();
            activity?.Stop();
        }
    }
}

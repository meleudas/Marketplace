using Marketplace.Application.Products.Ports;

namespace Marketplace.Infrastructure.Jobs;

public sealed class SearchIndexJobs
{
    private readonly IProductSearchIndexer _indexer;

    public SearchIndexJobs(IProductSearchIndexer indexer)
    {
        _indexer = indexer;
    }

    public Task UpsertProductAsync(long productId, CancellationToken ct = default)
        => _indexer.UpsertProductAsync(productId, ct);

    public Task DeleteProductAsync(long productId, CancellationToken ct = default)
        => _indexer.DeleteProductAsync(productId, ct);

    public Task FullReindexAsync(CancellationToken ct = default)
        => _indexer.FullReindexAsync(ct);
}

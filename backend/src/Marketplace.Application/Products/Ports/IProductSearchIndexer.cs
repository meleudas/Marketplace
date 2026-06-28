namespace Marketplace.Application.Products.Ports;

public interface IProductSearchIndexer
{
    Task UpsertProductAsync(long productId, CancellationToken ct = default);
    Task DeleteProductAsync(long productId, CancellationToken ct = default);
    Task FullReindexAsync(CancellationToken ct = default);
}

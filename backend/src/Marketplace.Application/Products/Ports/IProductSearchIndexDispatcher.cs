namespace Marketplace.Application.Products.Ports;

public interface IProductSearchIndexDispatcher
{
    Task EnqueueUpsertProductAsync(long productId, CancellationToken ct = default);
    Task EnqueueDeleteProductAsync(long productId, CancellationToken ct = default);
}

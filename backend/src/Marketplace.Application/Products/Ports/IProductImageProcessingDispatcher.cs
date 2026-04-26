namespace Marketplace.Application.Products.Ports;

public interface IProductImageProcessingDispatcher
{
    Task EnqueueDerivativesAsync(long productId, string originalObjectKey, CancellationToken ct = default);
}

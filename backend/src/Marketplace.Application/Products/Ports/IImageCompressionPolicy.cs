namespace Marketplace.Application.Products.Ports;

public interface IImageCompressionPolicy
{
    Task<(MemoryStream Stream, string ContentType)> EncodeDerivativeAsync(
        byte[] sourceBytes,
        int maxWidth,
        int maxHeight,
        long originalBytes,
        CancellationToken ct = default);
}

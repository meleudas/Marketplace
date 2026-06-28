using Marketplace.Application.Common.Ports;
using Marketplace.Application.Products.Ports;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Infrastructure.Jobs;

public sealed class ProductImageJobs
{
    private readonly IObjectStorage _storage;
    private readonly IProductImageRepository _productImageRepository;
    private readonly IImageCompressionPolicy _compressionPolicy;

    public ProductImageJobs(
        IObjectStorage storage,
        IProductImageRepository productImageRepository,
        IImageCompressionPolicy compressionPolicy)
    {
        _storage = storage;
        _productImageRepository = productImageRepository;
        _compressionPolicy = compressionPolicy;
    }

    public async Task ProcessDerivativesAsync(long productId, string originalObjectKey, CancellationToken ct = default)
    {
        await using var originalStream = await _storage.DownloadAsync(originalObjectKey, ct);
        await using var originalCopy = new MemoryStream();
        await originalStream.CopyToAsync(originalCopy, ct);
        var originalBytesBuffer = originalCopy.ToArray();
        var originalBytes = originalBytesBuffer.LongLength;
        var thumbResult = await _compressionPolicy.EncodeDerivativeAsync(originalBytesBuffer, 200, 200, originalBytes, ct);
        var mediumResult = await _compressionPolicy.EncodeDerivativeAsync(originalBytesBuffer, 800, 800, originalBytes, ct);
        try
        {
            var baseName = Path.GetFileNameWithoutExtension(originalObjectKey);
            var thumbKey = $"products/{productId}/derived/{baseName}_thumb.webp";
            var mediumKey = $"products/{productId}/derived/{baseName}_medium.webp";

            await _storage.UploadAsync(thumbKey, thumbResult.Stream, thumbResult.ContentType, ct);
            await _storage.UploadAsync(mediumKey, mediumResult.Stream, mediumResult.ContentType, ct);

            var originalUrl = _storage.GetPublicUrl(originalObjectKey);
            var images = await _productImageRepository.ListByProductIdAsync(ProductId.From(productId), ct);
            var target = images.FirstOrDefault(x => string.Equals(x.ImageUrl, originalUrl, StringComparison.OrdinalIgnoreCase));
            if (target is null)
                return;
            var oldImageObjectKey = target.ImageObjectKey;
            var oldThumbObjectKey = target.ThumbnailObjectKey;

            target.Update(
                _storage.GetPublicUrl(mediumKey),
                _storage.GetPublicUrl(thumbKey),
                originalObjectKey,
                mediumKey,
                thumbKey,
                target.AltText,
                target.SortOrder,
                target.IsMain,
                null,
                null,
                mediumResult.Stream.Length);

            await _productImageRepository.ReplaceForProductAsync(ProductId.From(productId), images, ct);
            if (!string.Equals(oldImageObjectKey, mediumKey, StringComparison.OrdinalIgnoreCase))
                await TryDeleteAsync(oldImageObjectKey, ct);
            if (!string.Equals(oldThumbObjectKey, thumbKey, StringComparison.OrdinalIgnoreCase))
                await TryDeleteAsync(oldThumbObjectKey, ct);
        }
        finally
        {
            await thumbResult.Stream.DisposeAsync();
            await mediumResult.Stream.DisposeAsync();
        }
    }

    private async Task TryDeleteAsync(string objectKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            return;
        try
        {
            await _storage.DeleteAsync(objectKey, ct);
        }
        catch
        {
            // Cleanup is best-effort; recurring cleanup job handles leftovers.
        }
    }
}

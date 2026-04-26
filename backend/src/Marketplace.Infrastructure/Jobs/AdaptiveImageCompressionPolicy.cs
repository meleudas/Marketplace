using Marketplace.Application.Products.Ports;
using Marketplace.Infrastructure.External.Storage;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Marketplace.Infrastructure.Jobs;

public sealed class AdaptiveImageCompressionPolicy : IImageCompressionPolicy
{
    private readonly StorageOptions _options;

    public AdaptiveImageCompressionPolicy(IOptions<StorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<(MemoryStream Stream, string ContentType)> EncodeDerivativeAsync(
        byte[] sourceBytes,
        int maxWidth,
        int maxHeight,
        long originalBytes,
        CancellationToken ct = default)
    {
        await using var sourceStream = new MemoryStream(sourceBytes);
        using var source = await Image.LoadAsync(sourceStream, ct);
        using var resized = source.Clone(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(maxWidth, maxHeight)
        }));

        var threshold = Math.Max(1, (long)(originalBytes * _options.DerivativeMaxToOriginalRatio));
        MemoryStream? winner = null;
        foreach (var quality in _options.DerivativeWebpQualities.OrderByDescending(x => x))
        {
            var candidate = new MemoryStream();
            await resized.SaveAsWebpAsync(candidate, new WebpEncoder { Quality = quality }, ct);
            if (candidate.Length <= threshold)
            {
                candidate.Position = 0;
                winner = candidate;
                break;
            }

            winner?.Dispose();
            winner = candidate;
        }

        winner ??= new MemoryStream();
        winner.Position = 0;
        return (winner, "image/webp");
    }
}

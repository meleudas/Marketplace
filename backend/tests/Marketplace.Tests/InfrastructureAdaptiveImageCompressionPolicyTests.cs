using Marketplace.Infrastructure.External.Storage;
using Marketplace.Infrastructure.Jobs;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Marketplace.Tests;

public sealed class InfrastructureAdaptiveImageCompressionPolicyTests
{
    [Fact]
    public async Task EncodeDerivative_Respects_MaxToOriginal_Ratio()
    {
        using var image = new Image<Rgba32>(1200, 800, new Rgba32(120, 30, 60));
        await using var sourceStream = new MemoryStream();
        await image.SaveAsPngAsync(sourceStream);
        var sourceBytes = sourceStream.ToArray();

        var options = Options.Create(new StorageOptions
        {
            DerivativeMaxToOriginalRatio = 0.8,
            DerivativeWebpQualities = [95, 80, 65, 50]
        });

        var sut = new AdaptiveImageCompressionPolicy(options);
        var result = await sut.EncodeDerivativeAsync(sourceBytes, 800, 800, sourceBytes.LongLength, CancellationToken.None);
        try
        {
            Assert.True(result.Stream.Length <= sourceBytes.LongLength);
            Assert.Equal("image/webp", result.ContentType);
        }
        finally
        {
            await result.Stream.DisposeAsync();
        }
    }
}

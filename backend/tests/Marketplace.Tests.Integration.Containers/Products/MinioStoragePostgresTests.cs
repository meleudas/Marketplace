using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Marketplace.Tests.Products;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "ProductsModeration")]
[Trait("Layer", "IntegrationContainers")]
public sealed class MinioStoragePostgresTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public MinioStoragePostgresTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Minio_Storage_Can_Put_And_Get_Object()
    {
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var storage = scope.ServiceProvider.GetRequiredService<IObjectStorage>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<StorageOptions>>().Value;
        Assert.True(options.Enabled);

        await storage.EnsureBucketExistsAsync(CancellationToken.None);

        var key = $"tests/{Guid.NewGuid():N}.txt";
        await using var stream = new MemoryStream("hello-minio"u8.ToArray());
        await storage.UploadAsync(key, stream, "text/plain", CancellationToken.None);

        var keys = await storage.ListKeysAsync("tests/", CancellationToken.None);
        Assert.Contains(key, keys);
    }
}

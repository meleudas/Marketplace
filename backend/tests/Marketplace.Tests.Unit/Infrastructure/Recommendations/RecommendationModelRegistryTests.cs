using Marketplace.Application.Common.Ports;
using Marketplace.Application.Products.Options;
using Marketplace.Application.Products.Ports;
using Marketplace.Infrastructure.External.Recommendations;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

public class RecommendationModelRegistryTests
{
    [Fact]
    [Trait("Suite", "Recommendations")]
    public async Task PromoteCandidate_MovesPointersCorrectly()
    {
        var storage = new InMemoryObjectStorage();
        var registry = new ObjectStorageRecommendationModelRegistry(
            storage,
            Options.Create(new RecommendationModelOptions { RegistryPrefix = "ml/recommendations/registry" }));

        var active = new RecommendationModelMetadata("v1", DateTime.UtcNow, DateTime.UtcNow.AddDays(-10), DateTime.UtcNow, 100, 0.5f, "v1", "ml/recommendations/v1/model.zip");
        var candidate = new RecommendationModelMetadata("v2", DateTime.UtcNow, DateTime.UtcNow.AddDays(-5), DateTime.UtcNow, 120, 0.4f, "v1", "ml/recommendations/v2/model.zip");

        await registry.SetCandidateAsync(active);
        await registry.PromoteCandidateAsync();
        await registry.SetCandidateAsync(candidate);
        await registry.PromoteCandidateAsync();

        var pointers = await registry.GetPointersAsync();
        Assert.NotNull(pointers.Active);
        Assert.NotNull(pointers.Previous);
        Assert.Equal("v2", pointers.Active!.Version);
        Assert.Equal("v1", pointers.Previous!.Version);
        Assert.Null(pointers.Candidate);
    }

    private sealed class InMemoryObjectStorage : IObjectStorage
    {
        private readonly Dictionary<string, byte[]> _store = new(StringComparer.Ordinal);

        public Task EnsureBucketExistsAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task UploadAsync(string objectKey, Stream content, string contentType, CancellationToken ct = default)
        {
            using var memory = new MemoryStream();
            content.CopyTo(memory);
            _store[objectKey] = memory.ToArray();
            return Task.CompletedTask;
        }

        public Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default)
        {
            if (!_store.TryGetValue(objectKey, out var bytes))
                throw new FileNotFoundException(objectKey);
            return Task.FromResult<Stream>(new MemoryStream(bytes));
        }

        public Task DeleteAsync(string objectKey, CancellationToken ct = default)
        {
            _store.Remove(objectKey);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<string>> ListKeysAsync(string? prefix = null, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<string>>(
                string.IsNullOrWhiteSpace(prefix)
                    ? _store.Keys.ToList()
                    : _store.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList());

        public string GetPublicUrl(string objectKey) => objectKey;

        public Task<string> GetPresignedGetUrlAsync(string objectKey, CancellationToken ct = default)
            => Task.FromResult(objectKey);
    }
}

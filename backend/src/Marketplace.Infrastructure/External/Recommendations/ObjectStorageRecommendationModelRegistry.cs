using System.Text;
using System.Text.Json;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Products.Options;
using Marketplace.Application.Products.Ports;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.External.Recommendations;

public sealed class ObjectStorageRecommendationModelRegistry : IRecommendationModelRegistry
{
    private readonly IObjectStorage _storage;
    private readonly RecommendationModelOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ObjectStorageRecommendationModelRegistry(
        IObjectStorage storage,
        IOptions<RecommendationModelOptions> options)
    {
        _storage = storage;
        _options = options.Value;
    }

    public async Task<RecommendationModelPointers> GetPointersAsync(CancellationToken ct = default)
    {
        var active = await ReadPointerAsync("active.json", ct);
        var candidate = await ReadPointerAsync("candidate.json", ct);
        var previous = await ReadPointerAsync("previous.json", ct);
        return new RecommendationModelPointers(active, candidate, previous);
    }

    public Task SetCandidateAsync(RecommendationModelMetadata metadata, CancellationToken ct = default)
        => WritePointerAsync("candidate.json", metadata, ct);

    public async Task PromoteCandidateAsync(CancellationToken ct = default)
    {
        var pointers = await GetPointersAsync(ct);
        if (pointers.Candidate is null)
            return;

        if (pointers.Active is not null)
            await WritePointerAsync("previous.json", pointers.Active, ct);
        await WritePointerAsync("active.json", pointers.Candidate, ct);
        await DeletePointerAsync("candidate.json", ct);
    }

    private async Task<RecommendationModelMetadata?> ReadPointerAsync(string fileName, CancellationToken ct)
    {
        var key = BuildRegistryKey(fileName);
        try
        {
            await using var stream = await _storage.DownloadAsync(key, ct);
            return await JsonSerializer.DeserializeAsync<RecommendationModelMetadata>(stream, JsonOptions, ct);
        }
        catch
        {
            return null;
        }
    }

    private async Task WritePointerAsync(string fileName, RecommendationModelMetadata metadata, CancellationToken ct)
    {
        var key = BuildRegistryKey(fileName);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(metadata, JsonOptions);
        await _storage.EnsureBucketExistsAsync(ct);
        await using var stream = new MemoryStream(bytes);
        await _storage.UploadAsync(key, stream, "application/json", ct);
    }

    private async Task DeletePointerAsync(string fileName, CancellationToken ct)
    {
        var key = BuildRegistryKey(fileName);
        try
        {
            await _storage.DeleteAsync(key, ct);
        }
        catch
        {
            // Ignore missing pointer file.
        }
    }

    private string BuildRegistryKey(string fileName)
        => $"{_options.RegistryPrefix.TrimEnd('/')}/{fileName}";
}

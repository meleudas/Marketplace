using Marketplace.Application.Common.Ports;
using Marketplace.Application.Products.Options;
using Marketplace.Application.Products.Ports;
using Microsoft.Extensions.Options;
using Microsoft.ML;

namespace Marketplace.Infrastructure.External.Recommendations;

public sealed class RecommendationModelLoader
{
    private readonly IObjectStorage _storage;
    private readonly IRecommendationModelRegistry _registry;
    private readonly RecommendationModelOptions _options;
    private readonly SemaphoreSlim _sync = new(1, 1);
    private readonly MLContext _mlContext = new(seed: 42);
    private RecommendationRuntimeModel? _cached;

    public RecommendationModelLoader(
        IObjectStorage storage,
        IRecommendationModelRegistry registry,
        IOptions<RecommendationModelOptions> options)
    {
        _storage = storage;
        _registry = registry;
        _options = options.Value;
    }

    public async Task<RecommendationRuntimeModel?> GetActiveAsync(CancellationToken ct = default)
    {
        var pointers = await _registry.GetPointersAsync(ct);
        var active = pointers.Active;
        if (active is null)
            return null;

        if (_cached is not null && string.Equals(_cached.Version, active.Version, StringComparison.Ordinal))
            return _cached;

        await _sync.WaitAsync(ct);
        try
        {
            if (_cached is not null && string.Equals(_cached.Version, active.Version, StringComparison.Ordinal))
                return _cached;

            await using var stream = await _storage.DownloadAsync(active.ArtifactKey, ct);
            var model = _mlContext.Model.Load(stream, out _);
            _cached = new RecommendationRuntimeModel(active.Version, model, _mlContext);
            return _cached;
        }
        catch
        {
            return null;
        }
        finally
        {
            _sync.Release();
        }
    }

    public Task InvalidateAsync()
    {
        _cached = null;
        return Task.CompletedTask;
    }
}

public sealed class RecommendationRuntimeModel
{
    public string Version { get; }
    public ITransformer Model { get; }
    public MLContext MlContext { get; }

    public RecommendationRuntimeModel(string version, ITransformer model, MLContext mlContext)
    {
        Version = version;
        Model = model;
        MlContext = mlContext;
    }
}

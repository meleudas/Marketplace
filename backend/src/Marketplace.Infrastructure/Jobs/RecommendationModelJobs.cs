using System.Text.Json;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Products.Options;
using Marketplace.Application.Products.Ports;
using Marketplace.Infrastructure.External.Recommendations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.Jobs;

public sealed class RecommendationModelJobs
{
    private readonly IRecommendationTrainingDataReader _trainingDataReader;
    private readonly IRecommendationModelTrainer _trainer;
    private readonly IRecommendationModelRegistry _registry;
    private readonly RecommendationModelLoader _loader;
    private readonly IObjectStorage _storage;
    private readonly RecommendationModelOptions _modelOptions;
    private readonly RecommendationTrainingOptions _trainingOptions;
    private readonly ILogger<RecommendationModelJobs> _logger;

    public RecommendationModelJobs(
        IRecommendationTrainingDataReader trainingDataReader,
        IRecommendationModelTrainer trainer,
        IRecommendationModelRegistry registry,
        RecommendationModelLoader loader,
        IObjectStorage storage,
        IOptions<RecommendationModelOptions> modelOptions,
        IOptions<RecommendationTrainingOptions> trainingOptions,
        ILogger<RecommendationModelJobs> logger)
    {
        _trainingDataReader = trainingDataReader;
        _trainer = trainer;
        _registry = registry;
        _loader = loader;
        _storage = storage;
        _modelOptions = modelOptions.Value;
        _trainingOptions = trainingOptions.Value;
        _logger = logger;
    }

    public async Task TrainAndValidateAsync(CancellationToken ct = default)
    {
        if (!_modelOptions.Enabled)
            return;

        var now = DateTime.UtcNow;
        var from = now.AddDays(-Math.Max(1, _trainingOptions.LookbackDays));
        var rows = await _trainingDataReader.ReadAsync(from, _trainingOptions.MaxTrainingRows, ct);
        if (rows.Count < _modelOptions.MinUserInteractions)
        {
            _logger.LogInformation("Skip ML.NET training, not enough rows: {Rows}", rows.Count);
            return;
        }

        var trainResult = await _trainer.TrainAsync(new RecommendationTrainingRequest(from, now, rows), ct);
        var artifactKey = $"{_modelOptions.ArtifactPrefix.TrimEnd('/')}/{trainResult.Metadata.Version}/model.zip";
        var metadataKey = $"{_modelOptions.ArtifactPrefix.TrimEnd('/')}/{trainResult.Metadata.Version}/metadata.json";

        await _storage.EnsureBucketExistsAsync(ct);
        await using (var zipStream = new MemoryStream(trainResult.ModelZipBytes))
            await _storage.UploadAsync(artifactKey, zipStream, "application/zip", ct);
        await using (var metaStream = new MemoryStream(trainResult.MetadataJsonBytes))
            await _storage.UploadAsync(metadataKey, metaStream, "application/json", ct);

        var metadata = trainResult.Metadata with { ArtifactKey = artifactKey };
        await _registry.SetCandidateAsync(metadata, ct);
        MarketplaceMetrics.RecommendationModelTrainings.Add(1);
    }

    public async Task PromoteCandidateAsync(CancellationToken ct = default)
    {
        if (!_modelOptions.Enabled)
            return;

        await _registry.PromoteCandidateAsync(ct);
        await _loader.InvalidateAsync();
        MarketplaceMetrics.RecommendationModelPromotions.Add(1);
    }

    public async Task PruneOldArtifactsAsync(CancellationToken ct = default)
    {
        if (!_modelOptions.Enabled)
            return;

        var pointers = await _registry.GetPointersAsync(ct);
        var keep = new HashSet<string>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(pointers.Active?.ArtifactKey))
            keep.Add(pointers.Active.ArtifactKey);
        if (!string.IsNullOrWhiteSpace(pointers.Candidate?.ArtifactKey))
            keep.Add(pointers.Candidate.ArtifactKey);
        if (!string.IsNullOrWhiteSpace(pointers.Previous?.ArtifactKey))
            keep.Add(pointers.Previous.ArtifactKey);

        var allKeys = await _storage.ListKeysAsync($"{_modelOptions.ArtifactPrefix.TrimEnd('/')}/", ct);
        foreach (var key in allKeys)
        {
            if (keep.Contains(key))
                continue;

            if (!key.EndsWith("/model.zip", StringComparison.OrdinalIgnoreCase) &&
                !key.EndsWith("/metadata.json", StringComparison.OrdinalIgnoreCase))
                continue;

            await _storage.DeleteAsync(key, ct);
        }
    }
}

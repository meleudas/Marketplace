using System.Text.Json;
using Marketplace.Application.Products.Options;
using Marketplace.Application.Products.Ports;
using Microsoft.Extensions.Options;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Marketplace.Infrastructure.External.Recommendations;

public sealed class MlNetRecommendationModelTrainer : IRecommendationModelTrainer
{
    private readonly MLContext _mlContext = new(seed: 42);
    private readonly RecommendationTrainingOptions _options;

    public MlNetRecommendationModelTrainer(IOptions<RecommendationTrainingOptions> options)
    {
        _options = options.Value;
    }

    public Task<RecommendationTrainingResult> TrainAsync(
        RecommendationTrainingRequest request,
        CancellationToken ct = default)
    {
        var limitedRows = request.Rows
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Take(Math.Max(1, _options.MaxTrainingRows))
            .Select(x => new MlRecommendationInput
            {
                UserId = x.UserId.ToString("N"),
                ProductId = x.ProductId.ToString(),
                Label = x.Label
            })
            .ToList();

        if (limitedRows.Count == 0)
            throw new InvalidOperationException("No training rows available for ML.NET trainer.");

        var data = _mlContext.Data.LoadFromEnumerable(limitedRows);
        var split = _mlContext.Data.TrainTestSplit(data, testFraction: Math.Clamp(_options.TrainTestSplit, 0.05f, 0.5f));

        var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("UserIdEncoded", nameof(MlRecommendationInput.UserId))
            .Append(_mlContext.Transforms.Conversion.MapValueToKey("ProductIdEncoded", nameof(MlRecommendationInput.ProductId)))
            .Append(_mlContext.Transforms.Categorical.OneHotEncoding("UserIdFeatures", "UserIdEncoded"))
            .Append(_mlContext.Transforms.Categorical.OneHotEncoding("ProductIdFeatures", "ProductIdEncoded"))
            .Append(_mlContext.Transforms.Concatenate("Features", "UserIdFeatures", "ProductIdFeatures"))
            .Append(_mlContext.Regression.Trainers.Sdca(
                labelColumnName: nameof(MlRecommendationInput.Label),
                featureColumnName: "Features"));

        var model = pipeline.Fit(split.TrainSet);
        var predictions = model.Transform(split.TestSet);
        var metrics = _mlContext.Regression.Evaluate(predictions, labelColumnName: nameof(MlRecommendationInput.Label), scoreColumnName: "Score");

        using var modelStream = new MemoryStream();
        _mlContext.Model.Save(model, split.TrainSet.Schema, modelStream);

        var version = $"mlnet-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var metadata = new RecommendationModelMetadata(
            version,
            DateTime.UtcNow,
            request.DataWindowFromUtc,
            request.DataWindowToUtc,
            limitedRows.Count,
            (float)metrics.RootMeanSquaredError,
            "v1",
            string.Empty);
        var metadataBytes = JsonSerializer.SerializeToUtf8Bytes(metadata);

        return Task.FromResult(new RecommendationTrainingResult(
            metadata,
            modelStream.ToArray(),
            metadataBytes));
    }

    private sealed class MlRecommendationInput
    {
        public string UserId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public float Label { get; set; }
    }
}

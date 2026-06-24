namespace Marketplace.Application.Products.Ports;

public sealed record RecommendationTrainingRequest(
    DateTime DataWindowFromUtc,
    DateTime DataWindowToUtc,
    IReadOnlyList<RecommendationTrainingRow> Rows);

public sealed record RecommendationTrainingResult(
    RecommendationModelMetadata Metadata,
    byte[] ModelZipBytes,
    byte[] MetadataJsonBytes);

public interface IRecommendationModelTrainer
{
    Task<RecommendationTrainingResult> TrainAsync(
        RecommendationTrainingRequest request,
        CancellationToken ct = default);
}

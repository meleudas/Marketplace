namespace Marketplace.Application.Products.Ports;

public sealed record RecommendationModelMetadata(
    string Version,
    DateTime TrainedAtUtc,
    DateTime DataWindowFromUtc,
    DateTime DataWindowToUtc,
    int TrainingRows,
    float EvaluationRmse,
    string SchemaVersion,
    string ArtifactKey);

public sealed record RecommendationModelPointers(
    RecommendationModelMetadata? Active,
    RecommendationModelMetadata? Candidate,
    RecommendationModelMetadata? Previous);

public interface IRecommendationModelRegistry
{
    Task<RecommendationModelPointers> GetPointersAsync(CancellationToken ct = default);
    Task SetCandidateAsync(RecommendationModelMetadata metadata, CancellationToken ct = default);
    Task PromoteCandidateAsync(CancellationToken ct = default);
}

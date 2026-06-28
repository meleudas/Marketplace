namespace Marketplace.Application.Products.Ports;

public sealed record RecommendationTrainingRow(
    Guid UserId,
    long ProductId,
    float Label,
    int ViewCount,
    int SearchCount,
    int FavoriteCount,
    int CartCount,
    int PurchaseCount,
    DateTime UpdatedAtUtc);

public interface IRecommendationTrainingDataReader
{
    Task<IReadOnlyList<RecommendationTrainingRow>> ReadAsync(
        DateTime sinceUtc,
        int limit,
        CancellationToken ct = default);
}

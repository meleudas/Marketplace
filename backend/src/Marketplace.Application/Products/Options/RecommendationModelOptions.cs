namespace Marketplace.Application.Products.Options;

public sealed class RecommendationModelOptions
{
    public const string SectionName = "RecommendationModel";

    public bool Enabled { get; set; }
    public bool ShadowModeEnabled { get; set; }
    public int TopK { get; set; } = 15;
    public int CandidatePoolSize { get; set; } = 300;
    public int MinUserInteractions { get; set; } = 3;
    public string RetrainCron { get; set; } = "0 */6 * * *";
    public string PromoteCron { get; set; } = "15 */6 * * *";
    public string CleanupCron { get; set; } = "30 3 * * *";
    public string ArtifactPrefix { get; set; } = "ml/recommendations";
    public string RegistryPrefix { get; set; } = "ml/recommendations/registry";
    public bool UsePersonalizedEndpoint { get; set; } = true;
    public string FallbackMode { get; set; } = "similar_products";
}

namespace Marketplace.Application.Products.Options;

public sealed class RecommendationTrainingOptions
{
    public const string SectionName = "RecommendationTraining";

    public int LookbackDays { get; set; } = 30;
    public float TrainTestSplit { get; set; } = 0.2f;
    public int MatrixFactorizationIterations { get; set; } = 30;
    public float MatrixFactorizationLearningRate { get; set; } = 0.1f;
    public float MatrixFactorizationLambda { get; set; } = 0.025f;
    public int MatrixFactorizationApproximationRank { get; set; } = 64;
    public int MaxTrainingRows { get; set; } = 500_000;
}

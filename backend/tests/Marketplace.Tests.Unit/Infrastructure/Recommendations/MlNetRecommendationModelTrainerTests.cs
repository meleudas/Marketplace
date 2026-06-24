using Marketplace.Application.Products.Options;
using Marketplace.Application.Products.Ports;
using Marketplace.Infrastructure.External.Recommendations;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

public class MlNetRecommendationModelTrainerTests
{
    [Fact]
    [Trait("Suite", "Recommendations")]
    public async Task TrainAsync_BuildsModelArtifactAndMetadata()
    {
        var trainer = new MlNetRecommendationModelTrainer(
            Options.Create(new RecommendationTrainingOptions
            {
                TrainTestSplit = 0.2f,
                MatrixFactorizationIterations = 10,
                MatrixFactorizationApproximationRank = 16
            }));

        var rows = new List<RecommendationTrainingRow>();
        for (var i = 0; i < 200; i++)
        {
            rows.Add(new RecommendationTrainingRow(
                Guid.Parse($"00000000-0000-0000-0000-{(i % 20 + 1).ToString().PadLeft(12, '0')}"),
                i % 40 + 1,
                (i % 10) + 1,
                i % 10,
                i % 6,
                i % 4,
                i % 3,
                i % 2,
                DateTime.UtcNow.AddMinutes(-i)));
        }

        var result = await trainer.TrainAsync(new RecommendationTrainingRequest(
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            rows));

        Assert.NotNull(result.Metadata);
        Assert.NotEmpty(result.Metadata.Version);
        Assert.True(result.Metadata.TrainingRows > 0);
        Assert.NotEmpty(result.ModelZipBytes);
        Assert.NotEmpty(result.MetadataJsonBytes);
    }
}

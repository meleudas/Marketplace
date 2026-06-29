using System.Net.Http.Headers;
using System.Text;
using Marketplace.Application.Behavior.Ports;
using Marketplace.Infrastructure.External.Analytics;
using Marketplace.Infrastructure.Jobs;
using Marketplace.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Marketplace.Tests.Analytics;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "Analytics")]
[Trait("Layer", "IntegrationContainers")]
public sealed class ClickHouseWarehousePipelineContainersTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public ClickHouseWarehousePipelineContainersTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Write_Events_Rebuild_Signals_And_Funnel_Then_Read_Training_Rows()
    {
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var warehouse = scope.ServiceProvider.GetRequiredService<IAnalyticsWarehouseWriter>();
        var aggregationJobs = scope.ServiceProvider.GetRequiredService<AnalyticsWarehouseAggregationJobs>();
        var trainingReader = scope.ServiceProvider.GetRequiredService<Marketplace.Application.Products.Ports.IRecommendationTrainingDataReader>();
        var clickHouseOptions = scope.ServiceProvider.GetRequiredService<IOptions<ClickHouseOptions>>().Value;

        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await warehouse.WriteEventAsync(new AnalyticsWarehouseEvent(
            Guid.NewGuid(), "ProductView", now, userId, "sess-1", 101, null, "catalog", 1, "{}", now), CancellationToken.None);
        await warehouse.WriteEventAsync(new AnalyticsWarehouseEvent(
            Guid.NewGuid(), "FavoriteAdd", now, userId, "sess-1", 101, null, "catalog", 1, "{}", now), CancellationToken.None);
        await warehouse.WriteEventAsync(new AnalyticsWarehouseEvent(
            Guid.NewGuid(), "AddToCart", now, userId, "sess-1", 101, null, "cart", 1, "{}", now), CancellationToken.None);

        await aggregationJobs.RebuildUserItemSignalsAsync(CancellationToken.None);
        await aggregationJobs.RebuildFunnelDailyAsync(CancellationToken.None);

        var rows = await trainingReader.ReadAsync(now.AddDays(-1), 100, CancellationToken.None);
        Assert.NotEmpty(rows);
        Assert.Contains(rows, x => x.UserId == userId && x.ProductId == 101 && x.Label > 0);

        var funnelCount = await QueryScalarAsync(
            clickHouseOptions,
            $"SELECT count() FROM {clickHouseOptions.Database}.analytics_funnel_daily");
        Assert.True(funnelCount >= 1);
    }

    private static async Task<long> QueryScalarAsync(ClickHouseOptions options, string sql)
    {
        using var client = new HttpClient { BaseAddress = new Uri(options.Url.TrimEnd('/') + "/") };
        client.Timeout = TimeSpan.FromSeconds(Math.Max(5, options.CommandTimeoutSeconds));
        if (!string.IsNullOrWhiteSpace(options.Username))
        {
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Username}:{options.Password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        }

        var uri = $"?database={Uri.EscapeDataString(options.Database)}&query={Uri.EscapeDataString(sql)}";
        using var response = await client.PostAsync(uri, new StringContent(string.Empty, Encoding.UTF8, "text/plain"));
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(body);

        return long.Parse(body.Trim(), System.Globalization.CultureInfo.InvariantCulture);
    }
}

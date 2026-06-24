using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.Infrastructure.Health;

public static class HealthCheckRegistrationExtensions
{
    public static IServiceCollection AddMarketplaceHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var hasRedis = !string.IsNullOrWhiteSpace(configuration.GetConnectionString("Redis"));
        var clickHouseEnabled = configuration.GetSection("ClickHouse").GetValue<bool>("Enabled");
        var recommendationEnabled = configuration.GetSection("RecommendationModel").GetValue<bool>("Enabled");

        var builder = services.AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready", "live"])
            .AddCheck<ElasticsearchHealthCheck>("elasticsearch", tags: ["ready"])
            .AddCheck<StorageHealthCheck>("storage", tags: ["ready"])
            .AddCheck<OutboxQueueHealthCheck>("queue", tags: ["ready"]);

        if (hasRedis)
            builder.AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);
        if (clickHouseEnabled)
            builder.AddCheck<ClickHouseHealthCheck>("clickhouse", tags: ["ready"]);
        if (recommendationEnabled)
            builder.AddCheck<RecommendationModelHealthCheck>("recommendation_model", tags: ["ready"]);

        return services;
    }
}

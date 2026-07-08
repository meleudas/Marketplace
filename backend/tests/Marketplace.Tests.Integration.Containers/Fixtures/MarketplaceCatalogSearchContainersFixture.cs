using Marketplace.Application;
using Marketplace.Infrastructure;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Tests.Common.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DotNet.Testcontainers.Builders;
using Testcontainers.Elasticsearch;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace Marketplace.Tests.Fixtures;

public sealed class MarketplaceCatalogSearchContainersFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("marketplace_catalog_search_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    private readonly ElasticsearchContainer _elasticsearch = new ElasticsearchBuilder()
        .WithImage("docker.elastic.co/elasticsearch/elasticsearch:8.14.3")
        .WithEnvironment("discovery.type", "single-node")
        .WithEnvironment("xpack.security.enabled", "false")
        .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
        .WithPortBinding(9200, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(9200)))
        .Build();

    public string PostgresConnectionString { get; private set; } = string.Empty;
    public string RedisConnectionString { get; private set; } = string.Empty;
    public string ElasticsearchUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _postgres.StartAsync(),
            _redis.StartAsync(),
            _elasticsearch.StartAsync());

        PostgresConnectionString = _postgres.GetConnectionString();
        RedisConnectionString = _redis.GetConnectionString();
        ElasticsearchUrl = $"http://{_elasticsearch.Hostname}:{_elasticsearch.GetMappedPublicPort(9200)}";

        var serviceProvider = CreateServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        await CatalogSearchFilterFixtureSeeder.SeedAsync(serviceProvider);
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(
            _postgres.DisposeAsync().AsTask(),
            _redis.DisposeAsync().AsTask(),
            _elasticsearch.DisposeAsync().AsTask());
    }

    public IServiceProvider CreateServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = PostgresConnectionString,
                ["ConnectionStrings:Redis"] = RedisConnectionString,
                ["Elasticsearch:Enabled"] = "true",
                ["Elasticsearch:Url"] = ElasticsearchUrl,
                ["Elasticsearch:ProductsIndex"] = "products-v2-catalog-search-test",
                ["Storage:Enabled"] = "false",
                ["Jwt:SecretKey"] = "TestContainers_SecretKey_AtLeast32CharsLong!!",
                ["Jwt:Issuer"] = "marketplace-test",
                ["Jwt:Audience"] = "marketplace-test",
                ["Identity:RequireConfirmedEmail"] = "false",
                ["Cors:AllowedOrigins:0"] = "http://localhost:3000",
                ["Frontend:BaseUrl"] = "http://localhost:3000",
                ["LiqPay:PublicKey"] = "sandbox_test",
                ["LiqPay:PrivateKey"] = "sandbox_test",
                ["Telegram:WebhookSecret"] = "test-webhook-secret",
                ["WebPush:Enabled"] = "false",
                ["AppNotifications:EmailEnabled"] = "false",
                ["AppNotifications:TelegramEnabled"] = "false",
                ["Shipping:Enabled"] = "false",
                ["Coupons:ReadEnabled"] = "false",
                ["Chats:Enabled"] = "false",
                ["BehaviorAnalytics:BehaviorTrackingEnabled"] = "false",
                ["ClickHouse:Enabled"] = "false",
                ["RecommendationModel:Enabled"] = "false",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }
}

[CollectionDefinition(nameof(MarketplaceCatalogSearchContainersCollection))]
public sealed class MarketplaceCatalogSearchContainersCollection : ICollectionFixture<MarketplaceCatalogSearchContainersFixture>
{
}

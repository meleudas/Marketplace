using Marketplace.Infrastructure;
using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using Testcontainers.Elasticsearch;
using Testcontainers.Minio;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace Marketplace.Tests.Fixtures;

public sealed class MarketplaceContainersFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("marketplace_test")
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

    private readonly MinioContainer _minio = new MinioBuilder()
        .WithImage("minio/minio:latest")
        .WithUsername("minioadmin")
        .WithPassword("minioadmin")
        .Build();

    public string PostgresConnectionString { get; private set; } = string.Empty;
    public string RedisConnectionString { get; private set; } = string.Empty;
    public string ElasticsearchUrl { get; private set; } = string.Empty;
    public string MinioEndpoint { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _postgres.StartAsync(),
            _redis.StartAsync(),
            _elasticsearch.StartAsync(),
            _minio.StartAsync());

        PostgresConnectionString = _postgres.GetConnectionString();
        RedisConnectionString = _redis.GetConnectionString();
        ElasticsearchUrl = $"http://{_elasticsearch.Hostname}:{_elasticsearch.GetMappedPublicPort(9200)}";
        MinioEndpoint = $"{_minio.Hostname}:{_minio.GetMappedPublicPort(9000)}";

        await using var scope = CreateServiceProvider().CreateAsyncScope();
        await Marketplace.Infrastructure.DependencyInjection.InitializeDatabaseAsync(scope.ServiceProvider);
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(
            _postgres.DisposeAsync().AsTask(),
            _redis.DisposeAsync().AsTask(),
            _elasticsearch.DisposeAsync().AsTask(),
            _minio.DisposeAsync().AsTask());
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
                ["Storage:Enabled"] = "true",
                ["Storage:Endpoint"] = MinioEndpoint,
                ["Storage:AccessKey"] = "minioadmin",
                ["Storage:SecretKey"] = "minioadmin",
                ["Storage:Bucket"] = "marketplace-media",
                ["Storage:UseSsl"] = "false",
                ["Storage:PublicBaseUrl"] = "http://localhost:9000",
                ["Jwt:SecretKey"] = "TestContainers_SecretKey_AtLeast32CharsLong!!",
                ["Jwt:Issuer"] = "marketplace-test",
                ["Jwt:Audience"] = "marketplace-test",
                ["Cors:AllowedOrigins:0"] = "http://localhost:3000",
                ["Frontend:BaseUrl"] = "http://localhost:3000",
                ["LiqPay:PublicKey"] = "sandbox_test",
                ["LiqPay:PrivateKey"] = "sandbox_test",
                ["Telegram:WebhookSecret"] = "test-webhook-secret",
                ["WebPush:Enabled"] = "false",
                ["AppNotifications:EmailEnabled"] = "false",
                ["AppNotifications:TelegramEnabled"] = "false",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }

    public async Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        var sp = CreateServiceProvider();
        var scope = sp.CreateAsyncScope();
        return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }
}

[CollectionDefinition(nameof(MarketplaceContainersCollection))]
public sealed class MarketplaceContainersCollection : ICollectionFixture<MarketplaceContainersFixture>
{
}

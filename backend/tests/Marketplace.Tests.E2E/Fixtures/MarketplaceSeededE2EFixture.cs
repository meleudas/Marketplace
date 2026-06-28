using System.Net.Http.Headers;
using System.Net.Http.Json;
using Marketplace.API.Controllers;
using Marketplace.Application.Auth.DTOs;
using Marketplace.Tests.Common.Seed;
using Microsoft.AspNetCore.Mvc.Testing;
using DotNet.Testcontainers.Builders;
using Testcontainers.Elasticsearch;
using Testcontainers.Minio;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace Marketplace.Tests.Fixtures;

public sealed class MarketplaceSeededE2EFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("marketplace_e2e_seed")
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

    public MarketplaceWebApplicationFactory Factory { get; private set; } = null!;
    public string PostgresConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _postgres.StartAsync(),
            _redis.StartAsync(),
            _elasticsearch.StartAsync(),
            _minio.StartAsync());

        PostgresConnectionString = _postgres.GetConnectionString();
        await TestDatabaseBootstrap.MigrateAsync(PostgresConnectionString);
        await TestSeedDataLoader.ApplyAsync(PostgresConnectionString);
        Factory = new MarketplaceWebApplicationFactory(BuildSettings());
        _ = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await Task.WhenAll(
            _postgres.DisposeAsync().AsTask(),
            _redis.DisposeAsync().AsTask(),
            _elasticsearch.DisposeAsync().AsTask(),
            _minio.DisposeAsync().AsTask());
    }

    public async Task<HttpClient> LoginSeedUserAsync(string email)
    {
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            AllowAutoRedirect = false,
        });

        var login = await client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(email, SeedTestConstants.Password, false, null),
            E2EJsonOptions.Default);

        if (!login.IsSuccessStatusCode)
        {
            var body = await login.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Seed login failed for {email}: {(int)login.StatusCode} {body}");
        }

        var tokens = await login.Content.ReadFromJsonAsync<AuthTokensDto>(E2EJsonOptions.Default)
            ?? throw new InvalidOperationException("Login response did not contain tokens.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return client;
    }

    private Dictionary<string, string?> BuildSettings() =>
        E2ESharedSettings.Build(
            PostgresConnectionString,
            _redis.GetConnectionString(),
            $"http://{_elasticsearch.Hostname}:{_elasticsearch.GetMappedPublicPort(9200)}",
            $"{_minio.Hostname}:{_minio.GetMappedPublicPort(9000)}");
}

[CollectionDefinition(nameof(MarketplaceSeededE2ECollection))]
public sealed class MarketplaceSeededE2ECollection : ICollectionFixture<MarketplaceSeededE2EFixture>
{
}

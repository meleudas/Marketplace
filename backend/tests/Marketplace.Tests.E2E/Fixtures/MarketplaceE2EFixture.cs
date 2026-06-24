using System.Net.Http.Headers;
using System.Net.Http.Json;
using Marketplace.API.Controllers;
using Marketplace.Application.Auth.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Marketplace.Infrastructure.Identity.Entities;
using Marketplace.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using DotNet.Testcontainers.Builders;
using Testcontainers.Elasticsearch;
using Testcontainers.Minio;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace Marketplace.Tests.Fixtures;

public sealed class MarketplaceE2EFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("marketplace_e2e")
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

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _postgres.StartAsync(),
            _redis.StartAsync(),
            _elasticsearch.StartAsync(),
            _minio.StartAsync());

        Factory = new MarketplaceWebApplicationFactory(BuildSettings());
        await Factory.InitializeDatabaseAsync();
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

    public async Task<(HttpClient Client, Guid UserId)> CreateAuthenticatedClientAsync(string role = "Buyer")
    {
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            AllowAutoRedirect = false,
        });
        var email = $"{Guid.NewGuid():N}@example.test";
        const string password = "StrongPass1!Aa";
        var userName = $"user_{Guid.NewGuid():N}"[..16];

        var register = await client.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest(email, password, userName, null),
            E2EJsonOptions.Default);
        if (!register.IsSuccessStatusCode)
        {
            var body = await register.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Register failed: {(int)register.StatusCode} {body}");
        }

        var tokens = await register.Content.ReadFromJsonAsync<AuthTokensDto>(E2EJsonOptions.Default)
            ?? throw new InvalidOperationException("Register response did not contain tokens.");

        if (string.IsNullOrWhiteSpace(tokens.AccessToken))
            throw new InvalidOperationException("Register response did not contain a valid access token.");

        if (!string.Equals(role, "Buyer", StringComparison.OrdinalIgnoreCase))
        {
            await using var scope = Factory.Services.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email)
                ?? throw new InvalidOperationException("Registered user was not found.");
            await userManager.AddToRoleAsync(user, role);
            var tokenPort = scope.ServiceProvider.GetRequiredService<Marketplace.Application.Auth.Ports.ITokenPort>();
            var refreshed = tokenPort.GenerateAccessToken(
                Marketplace.Domain.Users.ValueObjects.IdentityUserId.From(user.Id),
                email,
                [role]);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.Value);
            return (client, user.Id);
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var userId = await ResolveUserIdAsync(email);
        return (client, userId);
    }

    private async Task<Guid> ResolveUserIdAsync(string email)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        return user?.Id ?? throw new InvalidOperationException("User id could not be resolved.");
    }

    private Dictionary<string, string?> BuildSettings() =>
        E2ESharedSettings.Build(
            _postgres.GetConnectionString(),
            _redis.GetConnectionString(),
            $"http://{_elasticsearch.Hostname}:{_elasticsearch.GetMappedPublicPort(9200)}",
            $"{_minio.Hostname}:{_minio.GetMappedPublicPort(9000)}");
}

[CollectionDefinition(nameof(MarketplaceE2ECollection))]
public sealed class MarketplaceE2ECollection : ICollectionFixture<MarketplaceE2EFixture>
{
}

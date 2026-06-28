using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Marketplace.API.OpenApi;
using Marketplace.Tests.Common.Seed;
using Marketplace.Tests.Fixtures;
using Xunit;

namespace Marketplace.Tests.Smoke;

[Collection(nameof(MarketplaceSeededE2ECollection))]
[Trait("Suite", "ApiCatalogSmoke")]
[Trait("Layer", "E2E")]
public sealed class ApiEndpointCatalogSmokeTests
{
    private static readonly HashSet<string> SkippedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/integrations/liqpay/webhook",
        "/integrations/telegram/webhook",
        "/integrations/shipping/novaposhta/webhook",
    };

    private readonly MarketplaceSeededE2EFixture _fixture;

    public ApiEndpointCatalogSmokeTests(MarketplaceSeededE2EFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task All_Discovered_Endpoints_Return_Non_Server_Error()
    {
        var endpoints = EndpointActionCatalog.DiscoverFromAssembly(typeof(Program).Assembly)
            .Where(e => !ShouldSkip(e.Method, e.Path))
            .ToList();

        var admin = await _fixture.LoginSeedUserAsync(SeedTestConstants.AdminEmail);
        var seller = await _fixture.LoginSeedUserAsync(SeedTestConstants.SellerEmail);
        var buyer = await _fixture.LoginSeedUserAsync(SeedTestConstants.BuyerEmail);
        var anonymous = _fixture.Factory.CreateClient();

        var failures = new List<string>();
        foreach (var (method, path) in endpoints)
        {
            var resolved = SeedRouteParameterResolver.Resolve(path);
            var client = PickClient(resolved, admin, seller, buyer, anonymous);
            var request = BuildRequest(method, resolved, client);
            var response = await client.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                var body = await response.Content.ReadAsStringAsync();
                failures.Add($"{method} {resolved} -> 500: {body[..Math.Min(body.Length, 200)]}");
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    private static bool ShouldSkip(string method, string path)
    {
        if (SkippedPaths.Contains(path))
            return true;
        if (path.Contains("/hubs/", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.Contains("upload", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    private static HttpClient PickClient(string path, HttpClient admin, HttpClient seller, HttpClient buyer, HttpClient anonymous)
    {
        if (path.StartsWith("/admin/", StringComparison.OrdinalIgnoreCase))
            return admin;
        if (path.StartsWith("/companies/", StringComparison.OrdinalIgnoreCase))
            return seller;
        if (path.StartsWith("/me/", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/account/", StringComparison.OrdinalIgnoreCase))
            return buyer;
        if (path.StartsWith("/users/", StringComparison.OrdinalIgnoreCase))
            return admin;
        return anonymous;
    }

    private static HttpRequestMessage BuildRequest(string method, string path, HttpClient client)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST" or "PUT" or "PATCH")
        {
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        }

        if (RequiresIdempotencyKey(path))
            request.Headers.TryAddWithoutValidation("Idempotency-Key", $"catalog-smoke-{method}-{path.GetHashCode():x}");

        if (client.DefaultRequestHeaders.Authorization is not null)
            request.Headers.Authorization = client.DefaultRequestHeaders.Authorization;

        return request;
    }

    private static bool RequiresIdempotencyKey(string path) =>
        path.Contains("/checkout", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/shipments", StringComparison.OrdinalIgnoreCase) && path.Split('/').Length > 4
        || path.Contains("/returns", StringComparison.OrdinalIgnoreCase) && path.Contains("/orders/", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/status", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/cancel", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/inventory/receive", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/inventory/transfer", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/approve", StringComparison.OrdinalIgnoreCase);
}

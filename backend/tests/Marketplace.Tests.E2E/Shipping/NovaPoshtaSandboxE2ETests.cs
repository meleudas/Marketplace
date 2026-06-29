using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Marketplace.API.Controllers;
using Marketplace.Application.Auth.DTOs;
using Marketplace.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Marketplace.Tests.Shipping;

[Collection(nameof(MarketplaceE2ECollection))]
[Trait("Suite", "Staging")]
[Trait("Layer", "E2E")]
public sealed class NovaPoshtaSandboxE2ETests
{
    private readonly MarketplaceE2EFixture _fixture;

    public NovaPoshtaSandboxE2ETests(MarketplaceE2EFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Shipping_Quote_Returns_Response_When_NovaPoshta_Key_Configured()
    {
        var apiKey = Environment.GetEnvironmentVariable("NOVAPOSHTA_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
            return;

        var factory = _fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("NovaPoshta:Enabled", "true");
            builder.UseSetting("NovaPoshta:ApiKey", apiKey);
            builder.UseSetting("Shipping:Enabled", "true");
            builder.UseSetting("Shipping:NovaPoshtaEnabled", "true");
        });

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            AllowAutoRedirect = false,
        });

        var email = $"{Guid.NewGuid():N}@staging.test";
        var register = await client.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest(email, "StrongPass1!Aa", $"np_{Guid.NewGuid():N}"[..12], null),
            E2EJsonOptions.Default);
        register.EnsureSuccessStatusCode();
        var tokens = await register.Content.ReadFromJsonAsync<AuthTokensDto>(E2EJsonOptions.Default)
            ?? throw new InvalidOperationException("Missing auth tokens");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await client.PostAsJsonAsync(
            "/shipping/quote",
            new CalculateShippingQuoteRequest(
                ShippingMethodId: 1,
                FirstName: "Test",
                LastName: "Buyer",
                Phone: "+380501234567",
                Street: "Khreshchatyk 1",
                City: "Kyiv",
                State: "Kyiv",
                PostalCode: "01001",
                Country: "UA"),
            E2EJsonOptions.Default);

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}

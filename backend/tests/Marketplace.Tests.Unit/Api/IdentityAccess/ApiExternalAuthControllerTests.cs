using Marketplace.API.Controllers;
using Marketplace.API.Options;
using Marketplace.Application.Auth.DTOs;
using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "API")]
[Trait("Suite", "IdentityAccess")]
public class ApiExternalAuthControllerTests
{
    [Fact]
    public async Task Google_Returns_503_When_OAuth_Not_Configured()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var controller = new ExternalAuthController(
            configuration,
            new NullGoogleOAuthPort(),
            Options.Create(new CookieAuthOptions { RefreshTokenCookieName = "refresh_token", RefreshTokenDays = 30 }))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var response = await controller.Google("/auth/callback", CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Equal(503, objectResult.StatusCode);
    }

    [Fact]
    public async Task GoogleCallbackPost_Returns_BadRequest_When_Code_Missing()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["GoogleAuth:ClientId"] = "x"
        }).Build();
        var controller = new ExternalAuthController(
            configuration,
            new NullGoogleOAuthPort(),
            Options.Create(new CookieAuthOptions { RefreshTokenCookieName = "refresh_token", RefreshTokenDays = 30 }))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var response = await controller.GoogleCallbackPost(new GoogleCallbackExchangeRequest(" "), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(response);
    }

    private sealed class NullGoogleOAuthPort : IGoogleOAuthPort
    {
        public Task<string> CreateAuthStateAsync(string returnPath, CancellationToken ct = default) =>
            Task.FromResult("state");

        public Task<string?> ConsumeAuthStateAsync(string state, CancellationToken ct = default) =>
            Task.FromResult<string?>("/auth/callback");

        public Task<Result<AuthTokensDto>> SignInOrProvisionAsync(System.Security.Claims.ClaimsPrincipal principal, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<string> CreateExchangeCodeAsync(AuthTokensDto tokens, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<GoogleOAuthExchangePayload?> ConsumeExchangeCodeAsync(string code, CancellationToken ct = default) =>
            Task.FromResult<GoogleOAuthExchangePayload?>(null);
    }
}

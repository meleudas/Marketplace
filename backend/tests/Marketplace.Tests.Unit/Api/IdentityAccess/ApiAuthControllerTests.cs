using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.API.Options;
using Marketplace.Application.Auth.Commands.Login;
using Marketplace.Application.Auth.Commands.Logout;
using Marketplace.Application.Auth.Commands.RefreshToken;
using Marketplace.Application.Auth.Commands.Register;
using Marketplace.Application.Auth.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "API")]
[Trait("Suite", "IdentityAccess")]
public class ApiAuthControllerTests
{
    [Fact]
    public async Task Register_Sends_Command_And_Returns_Ok()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<AuthTokensDto>.Success(NewTokens())
        };
        var controller = BuildController(sender);

        var response = await controller.Register(new RegisterRequest("a@b.com", "StrongPass1!", "user"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(response);
        Assert.IsType<RegisterCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task Refresh_Takes_Token_From_Cookie_When_Body_Is_Empty()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<AuthTokensDto>.Success(NewTokens())
        };
        var controller = BuildController(sender);
        controller.ControllerContext.HttpContext.Items[Marketplace.API.Middleware.JwtCookieMiddleware.RefreshTokenItemKey] = "cookie-refresh";

        _ = await controller.Refresh(new RefreshRequest(null), CancellationToken.None);

        var command = Assert.IsType<RefreshTokenCommand>(sender.LastRequest);
        Assert.Equal("cookie-refresh", command.RefreshToken);
    }

    [Fact]
    public async Task Logout_Returns_Unauthorized_Without_Sub_Claim()
    {
        var controller = BuildController(new RecordingSender { NextResult = Result.Success() });
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var response = await controller.Logout(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(response);
    }

    private static AuthController BuildController(ISender sender)
    {
        var options = Options.Create(new CookieAuthOptions
        {
            RefreshTokenCookieName = "refresh_token",
            RefreshTokenDays = 30
        });

        var identity = new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "test");
        return new AuthController(sender, options, NullLogger<AuthController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    private static AuthTokensDto NewTokens()
        => new("access", "refresh", DateTime.UtcNow.AddMinutes(10), DateTime.UtcNow.AddDays(30));

    private sealed class RecordingSender : ISender
    {
        public object? LastRequest { get; private set; }
        public object? NextResult { get; set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult((TResponse)NextResult!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            LastRequest = request;
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(NextResult);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => Empty<TResponse>();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => Empty<object?>();

        private static async IAsyncEnumerable<T> Empty<T>()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}

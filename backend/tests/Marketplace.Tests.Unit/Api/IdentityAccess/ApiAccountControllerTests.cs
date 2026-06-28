using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Auth.Commands.TwoFactor.SendEmailTwoFactorCode;
using Marketplace.Application.Auth.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Marketplace.Tests;

[Trait("Suite", "API")]
[Trait("Suite", "IdentityAccess")]
public class ApiAccountControllerTests
{
    [Fact]
    public async Task ConfirmEmail_Sends_Command()
    {
        var sender = new RecordingSender { NextResult = Result.Success() };
        var controller = BuildController(sender);

        var response = await controller.ConfirmEmail(new ConfirmEmailRequest("u@e.com", "token"), CancellationToken.None);

        Assert.IsType<OkResult>(response);
    }

    [Fact]
    public async Task SendTwoFactorCode_Returns_Unauthorized_Without_Sub()
    {
        var sender = new RecordingSender { NextResult = Result.Success() };
        var controller = BuildController(sender);
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var response = await controller.SendTwoFactorCode(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(response);
    }

    [Fact]
    public async Task GetTwoFactorStatus_Returns_Ok()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<TwoFactorStatusDto>.Success(new TwoFactorStatusDto(true, false, false))
        };
        var controller = BuildController(sender);

        var response = await controller.GetTwoFactorStatus(CancellationToken.None);

        Assert.IsType<OkObjectResult>(response);
    }

    private static AccountController BuildController(ISender sender)
    {
        var identity = new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "test");
        return new AccountController(sender, NullLogger<AccountController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    private sealed class RecordingSender : ISender
    {
        public object? NextResult { get; set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            => Task.FromResult((TResponse)NextResult!);

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
            => Task.CompletedTask;

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => Task.FromResult(NextResult);

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

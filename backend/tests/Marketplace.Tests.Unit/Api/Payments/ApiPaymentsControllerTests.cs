using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Payments.Commands.RequestRefund;
using Marketplace.Application.Payments.Commands.SyncPaymentStatus;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Tests;

[Trait("Suite", "API")]
[Trait("Suite", "Payments")]
public sealed class ApiPaymentsControllerTests
{
    [Fact]
    public async Task Refund_Returns_Unauthorized_When_No_Sub_Claim()
    {
        var controller = new AdminPaymentsController(new RecordingSender { NextResult = Result.Success() })
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.Refund(11, new RequestRefundBody(10m, "test"), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Refund_Sends_RequestRefundCommand_For_Admin_User()
    {
        var sender = new RecordingSender { NextResult = Result.Success() };
        var controller = BuildAdminController(sender);

        var result = await controller.Refund(11, new RequestRefundBody(10m, "test"), CancellationToken.None);

        Assert.IsType<OkResult>(result);
        var command = Assert.IsType<RequestRefundCommand>(sender.LastRequest);
        Assert.Equal(11, command.PaymentId);
        Assert.Equal(10m, command.Amount);
        Assert.Equal("test", command.Reason);
    }

    [Fact]
    public async Task Sync_Sends_SyncPaymentStatusCommand_For_Admin_User()
    {
        var sender = new RecordingSender { NextResult = Result.Success() };
        var controller = BuildAdminController(sender);

        var result = await controller.Sync(12, CancellationToken.None);

        Assert.IsType<OkResult>(result);
        var command = Assert.IsType<SyncPaymentStatusCommand>(sender.LastRequest);
        Assert.Equal(12, command.PaymentId);
    }

    private static AdminPaymentsController BuildAdminController(ISender sender)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        ], "test");
        return new AdminPaymentsController(sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    private sealed class RecordingSender : ISender
    {
        public object? LastRequest { get; private set; }
        public object? NextResult { get; set; } = Result.Success();

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

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => Empty<TResponse>();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => Empty<object?>();

        private static async IAsyncEnumerable<T> Empty<T>()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}

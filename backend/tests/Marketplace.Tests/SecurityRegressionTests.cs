using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Orders.Commands.CancelOrder;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Tests;

public sealed class SecurityRegressionTests
{
    [Fact]
    [Trait("Suite", "Security")]
    public async Task Orders_ListMy_Returns_Unauthorized_When_No_Sub_Claim()
    {
        var controller = new OrdersController(new RecordingSender(), new StartedIdempotencyStore())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.ListMy(null, null, null, null, null, 1, 20, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    [Trait("Suite", "Security")]
    public async Task Cancel_Returns_BadRequest_When_Idempotency_Missing()
    {
        var sender = new RecordingSender { NextResult = Result.Success() };
        var controller = BuildAuthorizedOrdersController(sender);

        var result = await controller.Cancel(7, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    [Trait("Suite", "Security")]
    public async Task Webhook_Returns_Unauthorized_On_Failed_Command_Result()
    {
        var sender = new RecordingSender { NextResult = Result.Failure("Invalid LiqPay signature") };
        var controller = new PaymentsIntegrationsController(sender, new StartedIdempotencyStore())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.ControllerContext.HttpContext.Request.Headers["Idempotency-Key"] = "wh-sec-1";

        var result = await controller.Webhook(new LiqPayWebhookRequest("bad", "bad"), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    private static OrdersController BuildAuthorizedOrdersController(ISender sender)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        ], "test");
        return new OrdersController(sender, new StartedIdempotencyStore())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    private sealed class StartedIdempotencyStore : IHttpIdempotencyStore
    {
        public Task<HttpIdempotencyBeginResult> TryBeginAsync(string scope, string idempotencyKey, string requestHash, TimeSpan ttl, CancellationToken ct = default)
            => Task.FromResult(new HttpIdempotencyBeginResult(HttpIdempotencyBeginState.Started, null));

        public Task CompleteAsync(string scope, string idempotencyKey, string requestHash, int statusCode, string? responseBodyJson, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class RecordingSender : ISender
    {
        public object? NextResult { get; set; } = Result.Success();

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

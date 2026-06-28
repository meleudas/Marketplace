using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Carts.Commands.CheckoutCart;
using Marketplace.Application.Carts.DTOs;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Orders.Commands.CancelOrder;
using Marketplace.Application.Orders.DTOs;
using Marketplace.Application.Payments.Commands.HandleLiqPayWebhook;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Marketplace.Tests;

public sealed class ApiRegressionIdempotencyTests
{
    [Trait("Suite", "Platform")]
    [Fact]
    [Trait("Suite", "API")]
    [Trait("Suite", "CartCheckout")]
    public async Task Checkout_Without_IdempotencyKey_Returns_BadRequest()
    {
        var sender = new RecordingSender
        {
            NextGenericResult = Result<CheckoutResultDto>.Failure("failed")
        };
        var controller = BuildCartController(sender, new FixedIdempotencyStore(HttpIdempotencyBeginState.Started));

        var result = await controller.Checkout(
            new CheckoutCartRequest(
                "Card",
                1,
                new CheckoutAddressRequest("A", "B", "+380", "Street", "City", "State", "01001", "UA"),
                null),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Trait("Suite", "Platform")]
    [Fact]
    [Trait("Suite", "API")]
    [Trait("Suite", "CartCheckout")]
    public async Task Checkout_Replays_Stored_Response_When_Completed()
    {
        var sender = new RecordingSender
        {
            NextGenericResult = Result<CheckoutResultDto>.Failure("should not execute")
        };
        var store = new FixedIdempotencyStore(
            HttpIdempotencyBeginState.Completed,
            new HttpIdempotencyStoredResponse(200, """{"ok":true}"""));
        var controller = BuildCartController(sender, store);
        controller.ControllerContext.HttpContext.Request.Headers["Idempotency-Key"] = "idem-1";

        var result = await controller.Checkout(
            new CheckoutCartRequest(
                "Card",
                1,
                new CheckoutAddressRequest("A", "B", "+380", "Street", "City", "State", "01001", "UA"),
                null),
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.Null(sender.LastRequest);
    }

    [Trait("Suite", "Platform")]
    [Fact]
    [Trait("Suite", "API")]
    [Trait("Suite", "CartCheckout")]
    public async Task Checkout_With_InProgress_Idempotency_Returns_Conflict()
    {
        var sender = new RecordingSender
        {
            NextGenericResult = Result<CheckoutResultDto>.Failure("should not execute")
        };
        var controller = BuildCartController(sender, new FixedIdempotencyStore(HttpIdempotencyBeginState.InProgress));
        controller.ControllerContext.HttpContext.Request.Headers["Idempotency-Key"] = "idem-progress-1";

        var result = await controller.Checkout(
            new CheckoutCartRequest(
                "Card",
                1,
                new CheckoutAddressRequest("A", "B", "+380", "Street", "City", "State", "01001", "UA"),
                null),
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Null(sender.LastRequest);
    }

    [Trait("Suite", "Platform")]
    [Fact]
    [Trait("Suite", "API")]
    [Trait("Suite", "CartCheckout")]
    public async Task Checkout_With_RequestMismatch_Idempotency_Returns_Conflict()
    {
        var sender = new RecordingSender
        {
            NextGenericResult = Result<CheckoutResultDto>.Failure("should not execute")
        };
        var controller = BuildCartController(sender, new FixedIdempotencyStore(HttpIdempotencyBeginState.RequestMismatch));
        controller.ControllerContext.HttpContext.Request.Headers["Idempotency-Key"] = "idem-mismatch-1";

        var result = await controller.Checkout(
            new CheckoutCartRequest(
                "Card",
                1,
                new CheckoutAddressRequest("A", "B", "+380", "Street", "City", "State", "01001", "UA"),
                null),
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Null(sender.LastRequest);
    }

    [Trait("Suite", "Platform")]
    [Fact]
    [Trait("Suite", "API")]
    public async Task Cancel_Without_IdempotencyKey_Returns_BadRequest()
    {
        var sender = new RecordingSender { NextResult = Result.Success() };
        var controller = BuildOrdersController(sender, new FixedIdempotencyStore(HttpIdempotencyBeginState.Started));

        var result = await controller.Cancel(1, new CancelOrderRequest(OrderCancellationReasonCode.ChangedMind, null), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Trait("Suite", "Platform")]
    [Fact]
    [Trait("Suite", "API")]
    [Trait("Suite", "Payments")]
    public async Task Webhook_Uses_Provider_Native_Dedup_Without_Idempotency_Header()
    {
        var sender = new RecordingSender { NextResult = Result.Success() };
        var controller = new PaymentsIntegrationsController(sender, new FixedIdempotencyStore(HttpIdempotencyBeginState.Started))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Webhook(new LiqPayWebhookRequest("data", "signature"), CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.IsType<HandleLiqPayWebhookCommand>(sender.LastRequest);
    }

    [Trait("Suite", "Platform")]
    [Fact]
    [Trait("Suite", "API")]
    [Trait("Suite", "Payments")]
    public async Task Webhook_With_Stored_Replay_Returns_Stored_Status()
    {
        var sender = new RecordingSender { NextResult = Result.Success() };
        var store = new FixedIdempotencyStore(
            HttpIdempotencyBeginState.Completed,
            new HttpIdempotencyStoredResponse(401, null));
        var controller = new PaymentsIntegrationsController(sender, store)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Webhook(new LiqPayWebhookRequest("data", "signature"), CancellationToken.None);

        var statusCode = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(401, statusCode.StatusCode);
        Assert.Null(sender.LastRequest);
    }

    [Fact]
    [Trait("Suite", "API")]
    [Trait("Suite", "CartCheckout")]
    public async Task AddItem_Without_Sub_Claim_Returns_Unauthorized()
    {
        var sender = new RecordingSender { NextGenericResult = Result<CheckoutResultDto>.Failure("unused") };
        var controller = new CartController(sender, new FixedIdempotencyStore(HttpIdempotencyBeginState.Started), NullLogger<CartController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.AddItem(new AddCartItemRequest(1, 1), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    private static CartController BuildCartController(ISender sender, IHttpIdempotencyStore idempotencyStore)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", Guid.NewGuid().ToString())
        ], "test");
        return new CartController(sender, idempotencyStore, NullLogger<CartController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    private static OrdersController BuildOrdersController(ISender sender, IHttpIdempotencyStore idempotencyStore)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        ], "test");
        return new OrdersController(sender, idempotencyStore)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    private sealed class FixedIdempotencyStore : IHttpIdempotencyStore
    {
        private readonly HttpIdempotencyBeginState _state;
        private readonly HttpIdempotencyStoredResponse? _stored;

        public FixedIdempotencyStore(HttpIdempotencyBeginState state, HttpIdempotencyStoredResponse? stored = null)
        {
            _state = state;
            _stored = stored;
        }

        public Task<HttpIdempotencyBeginResult> TryBeginAsync(string scope, string idempotencyKey, string requestHash, TimeSpan ttl, CancellationToken ct = default)
            => Task.FromResult(new HttpIdempotencyBeginResult(_state, _stored));

        public Task CompleteAsync(string scope, string idempotencyKey, string requestHash, int statusCode, string? responseBodyJson, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class RecordingSender : ISender
    {
        public object? LastRequest { get; private set; }
        public object? NextResult { get; set; }
        public object? NextGenericResult { get; set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            var result = NextGenericResult is TResponse generic ? generic : (TResponse)NextResult!;
            return Task.FromResult(result);
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

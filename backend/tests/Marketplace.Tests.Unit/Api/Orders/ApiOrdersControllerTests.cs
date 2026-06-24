using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Orders.Commands.CancelOrder;
using Marketplace.Application.Orders.Commands.UpdateOrderStatus;
using Marketplace.Application.Orders.DTOs;
using Marketplace.Application.Orders.Queries.GetOrderById;
using Marketplace.Application.Orders.Queries.ListOrders;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Tests;

[Trait("Suite", "API")]
[Trait("Suite", "Orders")]
public class ApiOrdersControllerTests
{
    [Fact]
    public async Task ListMy_Sends_ListOrdersQuery()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<PagedOrdersDto>.Success(new PagedOrdersDto([], 0, 1, 20))
        };
        var controller = BuildController(sender);

        var result = await controller.ListMy(null, null, null, null, null, 1, 20, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<ListOrdersQuery>(sender.LastRequest);
    }

    [Fact]
    public async Task GetAdmin_Sends_GetOrderByIdQuery()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<OrderDetailsDto>.Failure("Order not found")
        };
        var controller = BuildController(sender);

        _ = await controller.GetAdmin(10, CancellationToken.None);

        Assert.IsType<GetOrderByIdQuery>(sender.LastRequest);
    }

    [Fact]
    public async Task Cancel_Sends_CancelOrderCommand()
    {
        var sender = new RecordingSender
        {
            NextResult = Result.Success()
        };
        var controller = BuildController(sender);

        controller.ControllerContext.HttpContext.Request.Headers["Idempotency-Key"] = "test-key";
        _ = await controller.Cancel(7, new CancelOrderRequest(OrderCancellationReasonCode.ChangedMind, null), CancellationToken.None);

        Assert.IsType<CancelOrderCommand>(sender.LastRequest);
        var cmd = (CancelOrderCommand)sender.LastRequest!;
        Assert.Equal(OrderCancellationReasonCode.ChangedMind, cmd.ReasonCode);
    }

    [Fact]
    public async Task ListMy_Returns_Unauthorized_When_No_Sub_Claim()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<PagedOrdersDto>.Success(new PagedOrdersDto([], 0, 1, 20))
        };
        var controller = new OrdersController(sender, new StartedIdempotencyStore())
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
    public async Task UpdateStatus_Returns_BadRequest_When_Idempotency_Missing()
    {
        var sender = new RecordingSender { NextResult = Result.Success() };
        var controller = BuildController(sender);

        var result = await controller.UpdateStatus(11, new UpdateOrderStatusRequest("Processing", null), CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Idempotency-Key header is required.", bad.Value);
    }

    [Fact]
    public async Task UpdateStatus_Returns_BadRequest_On_Invalid_Status()
    {
        var sender = new RecordingSender { NextResult = Result.Success() };
        var controller = BuildController(sender);
        controller.ControllerContext.HttpContext.Request.Headers["Idempotency-Key"] = "orders-idem-1";

        var result = await controller.UpdateStatus(11, new UpdateOrderStatusRequest("WrongStatus", null), CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid status", bad.Value);
    }

    [Fact]
    public async Task UpdateStatus_Sends_UpdateOrderStatusCommand()
    {
        var sender = new RecordingSender { NextResult = Result.Success() };
        var controller = BuildController(sender);
        controller.ControllerContext.HttpContext.Request.Headers["Idempotency-Key"] = "orders-idem-2";

        var result = await controller.UpdateStatus(11, new UpdateOrderStatusRequest("Processing", null), CancellationToken.None);

        Assert.IsType<OkResult>(result);
        var command = Assert.IsType<UpdateOrderStatusCommand>(sender.LastRequest);
        Assert.Equal(11, command.OrderId);
        Assert.Equal(OrderStatus.Processing, command.NewStatus);
    }

    [Fact]
    public async Task Cancel_Returns_BadRequest_When_Idempotency_Missing()
    {
        var sender = new RecordingSender { NextResult = Result.Success() };
        var controller = BuildController(sender);

        var result = await controller.Cancel(7, new CancelOrderRequest(OrderCancellationReasonCode.ChangedMind, null), CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Idempotency-Key header is required.", bad.Value);
    }

    private static OrdersController BuildController(ISender sender)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        }, "test");

        var controller = new OrdersController(sender, new StartedIdempotencyStore())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
        return controller;
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

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => Empty<TResponse>();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => Empty<object?>();

        private static async IAsyncEnumerable<T> Empty<T>()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}

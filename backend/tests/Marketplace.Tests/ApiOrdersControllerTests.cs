using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Orders.Commands.CancelOrder;
using Marketplace.Application.Orders.Commands.UpdateOrderStatus;
using Marketplace.Application.Orders.DTOs;
using Marketplace.Application.Orders.Queries.GetOrderById;
using Marketplace.Application.Orders.Queries.ListOrders;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Tests;

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

        _ = await controller.Cancel(7, CancellationToken.None);

        Assert.IsType<CancelOrderCommand>(sender.LastRequest);
    }

    private static OrdersController BuildController(ISender sender)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        }, "test");

        return new OrdersController(sender)
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

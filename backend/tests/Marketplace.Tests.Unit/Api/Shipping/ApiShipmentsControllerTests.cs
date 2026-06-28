using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Shipping.DTOs;
using Marketplace.Application.Shipping.Options;
using Marketplace.Application.Shipping.Queries.GetShipmentById;
using Marketplace.Application.Shipping.Queries.ListMyShipments;
using Marketplace.Application.Shipping.Queries.ListOrderShipments;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "API")]
[Trait("Suite", "Shipping")]
public sealed class ApiShipmentsControllerTests
{
    [Fact]
    public async Task List_Sends_Query()
    {
        var sender = new RecordingSender { NextResult = Result<IReadOnlyList<ShipmentDto>>.Success([]) };
        var controller = BuildController(sender);

        var result = await controller.List(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<ListMyShipmentsQuery>(sender.LastRequest);
    }

    [Fact]
    public async Task GetById_Sends_Query()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<ShipmentDetailDto>.Success(new ShipmentDetailDto(
                1, 2, 1, 1, "Courier", "Created", null, null, DateTime.UtcNow, DateTime.UtcNow, [], []))
        };
        var controller = BuildController(sender);

        var result = await controller.GetById(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<GetShipmentByIdQuery>(sender.LastRequest);
    }

    [Fact]
    public async Task ListForOrder_Sends_Query()
    {
        var sender = new RecordingSender { NextResult = Result<IReadOnlyList<ShipmentSummaryDto>>.Success([]) };
        var controller = BuildController(sender);

        var result = await controller.ListForOrder(2, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<ListOrderShipmentsQuery>(sender.LastRequest);
    }

    private static ShipmentsController BuildController(ISender sender)
    {
        var identity = new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "test");
        return new ShipmentsController(sender, Options.Create(new ShippingOptions { Enabled = true }))
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

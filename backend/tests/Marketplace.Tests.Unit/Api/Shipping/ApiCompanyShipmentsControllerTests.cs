using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Shipping.Commands.CreateShipment;
using Marketplace.Application.Shipping.DTOs;
using Marketplace.Application.Shipping.Options;
using Marketplace.Application.Shipping.Queries.GetShipmentById;
using Marketplace.Application.Shipping.Queries.ListOrderShipments;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "API")]
[Trait("Suite", "Shipping")]
public sealed class ApiCompanyShipmentsControllerTests
{
    [Fact]
    public async Task ListForOrder_Sends_Query()
    {
        var sender = new RecordingSender { NextResult = Result<IReadOnlyList<ShipmentSummaryDto>>.Success([]) };
        var controller = BuildController(sender);

        var result = await controller.ListForOrder(Guid.NewGuid(), 2, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<ListOrderShipmentsQuery>(sender.LastRequest);
    }

    [Fact]
    public async Task Create_Returns_BadRequest_When_Idempotency_Missing()
    {
        var sender = new RecordingSender { NextResult = Result<ShipmentDetailDto>.Failure("unused") };
        var controller = BuildController(sender);

        var result = await controller.Create(
            Guid.NewGuid(),
            2,
            new CreateShipmentRequest(1, [], "TRK"),
            CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Idempotency-Key header is required.", bad.Value);
    }

    [Fact]
    public async Task Create_Sends_Command_When_Idempotency_Present()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<ShipmentDetailDto>.Success(new ShipmentDetailDto(
                1, 2, 1, 1, "Courier", "Created", null, null, DateTime.UtcNow, DateTime.UtcNow, [], []))
        };
        var controller = BuildController(sender);
        controller.ControllerContext.HttpContext.Request.Headers["Idempotency-Key"] = "ship-idem-1";

        var result = await controller.Create(
            Guid.NewGuid(),
            2,
            new CreateShipmentRequest(1, [new CreateShipmentLineDto(1, 1)], "TRK"),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<CreateShipmentCommand>(sender.LastRequest);
    }

    private static CompanyShipmentsController BuildController(ISender sender)
    {
        var identity = new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "test");
        return new CompanyShipmentsController(
            sender,
            new StartedIdempotencyStore(),
            Options.Create(new ShippingOptions { Enabled = true }))
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

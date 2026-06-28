using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Returns.DTOs;
using Marketplace.Application.Returns.Queries.GetReturnById;
using Marketplace.Application.Returns.Queries.ListMyReturns;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Tests;

[Trait("Suite", "API")]
[Trait("Suite", "Returns")]
public sealed class ApiReturnsControllerTests
{
    [Fact]
    public async Task List_Sends_Query()
    {
        var sender = new RecordingSender { NextResult = Result<IReadOnlyList<ReturnRequestSummaryDto>>.Success([]) };
        var controller = BuildController(sender);

        var result = await controller.List(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<ListMyReturnsQuery>(sender.LastRequest);
    }

    [Fact]
    public async Task Get_Sends_Query()
    {
        var sender = new RecordingSender { NextResult = Result<ReturnRequestDetailDto>.Failure("not found") };
        var controller = BuildController(sender);

        _ = await controller.Get(1, CancellationToken.None);

        Assert.IsType<GetReturnByIdQuery>(sender.LastRequest);
    }

    private static ReturnsController BuildController(ISender sender)
    {
        var identity = new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "test");
        return new ReturnsController(sender, new NoopIdempotency())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    private sealed class NoopIdempotency : Marketplace.Application.Common.Ports.IHttpIdempotencyStore
    {
        public Task<Marketplace.Application.Common.Ports.HttpIdempotencyBeginResult> TryBeginAsync(string scope, string idempotencyKey, string requestHash, TimeSpan ttl, CancellationToken ct = default)
            => Task.FromResult(new Marketplace.Application.Common.Ports.HttpIdempotencyBeginResult(Marketplace.Application.Common.Ports.HttpIdempotencyBeginState.Started, null));

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

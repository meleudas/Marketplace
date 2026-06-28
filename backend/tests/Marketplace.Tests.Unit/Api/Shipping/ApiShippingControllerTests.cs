using Marketplace.API.Controllers;
using Marketplace.Application.Shipping.DTOs;
using Marketplace.Application.Shipping.Options;
using Marketplace.Application.Shipping.Queries.GetShippingMethods;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "API")]
[Trait("Suite", "Shipping")]
public sealed class ApiShippingControllerTests
{
    [Fact]
    public async Task GetMethods_Sends_Query()
    {
        var sender = new RecordingSender { NextResult = Result<IReadOnlyList<ShippingMethodDto>>.Success([]) };
        var controller = new ShippingController(sender, Options.Create(new ShippingOptions { Enabled = true }));

        var result = await controller.GetMethods(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<GetShippingMethodsQuery>(sender.LastRequest);
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

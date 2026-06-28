using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Shipping.Commands.CreateUserAddress;
using Marketplace.Application.Shipping.DTOs;
using Marketplace.Application.Shipping.Options;
using Marketplace.Application.Shipping.Queries.ListMyAddresses;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "API")]
[Trait("Suite", "Shipping")]
public sealed class ApiAddressesControllerTests
{
    [Fact]
    public async Task List_Sends_Query()
    {
        var sender = new RecordingSender { NextResult = Result<IReadOnlyList<UserAddressDto>>.Success([]) };
        var controller = BuildController(sender);

        var result = await controller.List(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<ListMyAddressesQuery>(sender.LastRequest);
    }

    [Fact]
    public async Task Create_Sends_Command()
    {
        var sender = new RecordingSender { NextResult = Result<UserAddressDto>.Success(new UserAddressDto(1, "Shipping", true, "A", "B", "+380", "S", "Kyiv", "Kyiv", "01001", "UA")) };
        var controller = BuildController(sender);

        var result = await controller.Create(new UpsertAddressRequest("Shipping", true, "A", "B", "+380", "S", "Kyiv", "Kyiv", "01001", "UA"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<CreateUserAddressCommand>(sender.LastRequest);
    }

    private static AddressesController BuildController(ISender sender)
    {
        var identity = new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "test");
        return new AddressesController(sender, Options.Create(new ShippingOptions { Enabled = true }))
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

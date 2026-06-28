using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Products.Commands.ApproveProduct;
using Marketplace.Application.Products.Commands.RejectProduct;
using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Queries.GetPendingProducts;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Tests;

[Trait("Suite", "API")]
[Trait("Suite", "ProductsModeration")]
public sealed class ApiAdminProductsControllerTests
{
    [Fact]
    public async Task GetPending_Sends_Query()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<IReadOnlyList<PendingProductModerationDto>>.Success([])
        };
        var controller = BuildController(sender);

        var result = await controller.GetPending(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<GetPendingProductsQuery>(sender.LastRequest);
    }

    [Fact]
    public async Task Approve_Sends_Command()
    {
        var sender = new RecordingSender { NextResult = Result.Success() };
        var controller = BuildController(sender);

        var result = await controller.Approve(12, CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.IsType<ApproveProductCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task Reject_Sends_Command()
    {
        var sender = new RecordingSender { NextResult = Result.Success() };
        var controller = BuildController(sender);

        var result = await controller.Reject(12, new RejectProductBody("bad content"), CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.IsType<RejectProductCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task Reject_Returns_Unauthorized_When_Sub_Missing()
    {
        var controller = new AdminProductsController(new RecordingSender())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.Reject(15, new RejectProductBody("reason"), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    private static AdminProductsController BuildController(ISender sender)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        ], "test");

        return new AdminProductsController(sender)
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

using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Products.Queries.GetCompanyProducts;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Tests;

public class ApiProductsControllerTests
{
    [Fact]
    public async Task GetCompanyProducts_Sends_Query()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<IReadOnlyList<Marketplace.Application.Products.DTOs.ProductListItemDto>>
                .Success(Array.Empty<Marketplace.Application.Products.DTOs.ProductListItemDto>())
        };
        var controller = BuildController(sender);
        var result = await controller.GetCompanyProducts(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<GetCompanyProductsQuery>(sender.LastRequest);
    }

    private static ProductsController BuildController(ISender sender)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        }, "test");
        return new ProductsController(sender)
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

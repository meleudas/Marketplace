using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Products.Commands.UploadProductImage;
using Marketplace.Application.Products.Queries.GetCompanyProducts;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Marketplace.Infrastructure.External.Storage;
using Marketplace.Application.Products.DTOs;

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

    [Fact]
    public async Task UploadImage_Returns_BadRequest_For_Unsupported_ContentType()
    {
        var sender = new RecordingSender();
        var controller = BuildController(sender, new StorageOptions { Enabled = true, MaxUploadBytes = 1024 * 1024 });
        var file = BuildFormFile("bad.gif", "image/gif", new byte[10]);

        var result = await controller.UploadImage(Guid.NewGuid(), 1, file, "alt", 0, false, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadImage_Sends_Command_When_Valid()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<ProductImageDto>.Success(new ProductImageDto("a", "", "alt", 0, false, null, null, 12, "processing"))
        };
        var controller = BuildController(sender, new StorageOptions { Enabled = true, MaxUploadBytes = 1024 * 1024 });
        var file = BuildFormFile("ok.jpg", "image/jpeg", new byte[20]);

        var result = await controller.UploadImage(Guid.NewGuid(), 5, file, "alt", 1, true, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<UploadProductImageCommand>(sender.LastRequest);
    }

    private static ProductsController BuildController(ISender sender, StorageOptions? storageOptions = null)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        }, "test");
        return new ProductsController(sender, Options.Create(storageOptions ?? new StorageOptions()))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    private static IFormFile BuildFormFile(string fileName, string contentType, byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
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

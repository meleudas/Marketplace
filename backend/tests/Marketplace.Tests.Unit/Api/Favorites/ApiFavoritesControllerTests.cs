using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Favorites.Commands.AddFavoriteProduct;
using Marketplace.Application.Favorites.Commands.RemoveFavoriteProduct;
using Marketplace.Application.Favorites.DTOs;
using Marketplace.Application.Favorites.Queries.GetMyFavorites;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Marketplace.Tests;

[Trait("Suite", "API")]
[Trait("Suite", "Favorites")]
public class ApiFavoritesControllerTests
{
    [Fact]
    public async Task GetMyFavorites_Sends_Query_And_Returns_Ok()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<IReadOnlyList<FavoriteItemDto>>.Success([
                new FavoriteItemDto(1, 11, DateTime.UtcNow, 10m, true)
            ])
        };
        var controller = BuildControllerWithUser(sender);

        var response = await controller.GetMyFavorites(CancellationToken.None);

        Assert.IsType<OkObjectResult>(response);
        Assert.IsType<GetMyFavoritesQuery>(sender.LastRequest);
    }

    [Fact]
    public async Task Add_Sends_Command_And_Returns_Ok()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<FavoriteItemDto>.Success(new FavoriteItemDto(2, 22, DateTime.UtcNow, 12m, true))
        };
        var controller = BuildControllerWithUser(sender);

        var response = await controller.Add(22, CancellationToken.None);

        Assert.IsType<OkObjectResult>(response);
        Assert.IsType<AddFavoriteProductCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task Remove_Sends_Command_And_Returns_Ok()
    {
        var sender = new RecordingSender { NextResult = Result<bool>.Success(true) };
        var controller = BuildControllerWithUser(sender);

        var response = await controller.Remove(33, CancellationToken.None);

        Assert.IsType<OkObjectResult>(response);
        Assert.IsType<RemoveFavoriteProductCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task Add_Returns_NotFound_When_Product_Not_Found()
    {
        var sender = new RecordingSender { NextResult = Result<FavoriteItemDto>.Failure("Product not found") };
        var controller = BuildControllerWithUser(sender);

        var response = await controller.Add(44, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Equal(404, objectResult.StatusCode);
    }

    [Fact]
    public async Task Favorites_Endpoints_Return_Unauthorized_Without_Sub()
    {
        var sender = new RecordingSender { NextResult = Result<bool>.Success(true) };
        var controller = new FavoritesController(sender, NullLogger<FavoritesController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var getResponse = await controller.GetMyFavorites(CancellationToken.None);
        var addResponse = await controller.Add(1, CancellationToken.None);
        var removeResponse = await controller.Remove(1, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(getResponse);
        Assert.IsType<UnauthorizedResult>(addResponse);
        Assert.IsType<UnauthorizedResult>(removeResponse);
    }

    private static FavoritesController BuildControllerWithUser(ISender sender)
    {
        var identity = new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "test");
        return new FavoritesController(sender, NullLogger<FavoritesController>.Instance)
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

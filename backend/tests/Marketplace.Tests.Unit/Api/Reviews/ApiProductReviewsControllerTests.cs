using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Reviews.Commands.CreateProductReview;
using Marketplace.Application.Reviews.Commands.DeleteOwnProductReview;
using Marketplace.Application.Reviews.Commands.UpdateOwnProductReview;
using Marketplace.Application.Reviews.DTOs;
using Marketplace.Application.Reviews.Queries.GetProductReviews;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Tests;

[Trait("Suite", "API")]
[Trait("Suite", "Reviews")]
public sealed class ApiProductReviewsControllerTests
{
    [Fact]
    public async Task List_Sends_Query()
    {
        var sender = new RecordingSender { NextResult = Result<ReviewListDto>.Success(new ReviewListDto(1, 20, [])) };
        var controller = BuildController(sender);

        var result = await controller.List(12, 1, 20, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<GetProductReviewsQuery>(sender.LastRequest);
    }

    [Fact]
    public async Task Create_Returns_Unauthorized_When_Sub_Missing()
    {
        var controller = new ProductReviewsController(new RecordingSender())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.Create(12, new UpsertProductReviewRequest(5, "t", "c"), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Update_And_Delete_Send_Route_Scoped_Commands()
    {
        var sender = new RecordingSender();
        var controller = BuildController(sender);

        sender.NextResult = Result<ReviewDto>.Success(new ReviewDto(1, "product", 12, null, Guid.NewGuid(), "u", 5, null, "t", "c", true, 1, DateTime.UtcNow, DateTime.UtcNow, null));
        var updateResult = await controller.Update(12, 33, new UpsertProductReviewRequest(5, "t", "c"), CancellationToken.None);
        Assert.IsType<OkObjectResult>(updateResult);
        var updateCommand = Assert.IsType<UpdateOwnProductReviewCommand>(sender.LastRequest);
        Assert.Equal(12, updateCommand.ProductId);

        sender.NextResult = Result.Success();
        var deleteResult = await controller.Delete(12, 33, CancellationToken.None);
        Assert.IsType<OkResult>(deleteResult);
        var deleteCommand = Assert.IsType<DeleteOwnProductReviewCommand>(sender.LastRequest);
        Assert.Equal(12, deleteCommand.ProductId);
    }

    private static ProductReviewsController BuildController(ISender sender)
    {
        var identity = new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "test");
        return new ProductReviewsController(sender)
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

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => Empty<TResponse>();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => Empty<object?>();

        private static async IAsyncEnumerable<T> Empty<T>()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}

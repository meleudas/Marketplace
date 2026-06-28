using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Reviews.Commands.UpsertCompanyReviewReply;
using Marketplace.Application.Reviews.Commands.UpsertProductReviewReply;
using Marketplace.Application.Reviews.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Tests;

[Trait("Suite", "API")]
[Trait("Suite", "Reviews")]
public sealed class ApiReviewRepliesControllerTests
{
    [Fact]
    public async Task UpsertProductReply_Returns_Unauthorized_When_Sub_Missing()
    {
        var controller = new ReviewRepliesController(new RecordingSender())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.UpsertProductReply(10, new UpsertReviewReplyRequest("body"), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task UpsertProduct_And_Company_Send_Commands()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<ReviewReplyDto>.Success(new ReviewReplyDto(1, Guid.NewGuid(), Guid.NewGuid(), "b", false, DateTime.UtcNow, DateTime.UtcNow))
        };
        var controller = BuildController(sender);

        var productResult = await controller.UpsertProductReply(10, new UpsertReviewReplyRequest("body"), CancellationToken.None);
        Assert.IsType<OkObjectResult>(productResult);
        Assert.IsType<UpsertProductReviewReplyCommand>(sender.LastRequest);

        var companyResult = await controller.UpsertCompanyReply(20, new UpsertReviewReplyRequest("body"), CancellationToken.None);
        Assert.IsType<OkObjectResult>(companyResult);
        Assert.IsType<UpsertCompanyReviewReplyCommand>(sender.LastRequest);
    }

    private static ReviewRepliesController BuildController(ISender sender)
    {
        var identity = new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "test");
        return new ReviewRepliesController(sender)
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

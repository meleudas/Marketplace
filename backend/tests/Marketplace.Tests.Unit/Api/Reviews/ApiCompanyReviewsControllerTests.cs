using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Reviews.Commands.CreateCompanyReview;
using Marketplace.Application.Reviews.Commands.DeleteOwnCompanyReview;
using Marketplace.Application.Reviews.Commands.UpdateOwnCompanyReview;
using Marketplace.Application.Reviews.DTOs;
using Marketplace.Application.Reviews.Queries.GetCompanyReviews;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Tests;

[Trait("Suite", "API")]
[Trait("Suite", "Reviews")]
public sealed class ApiCompanyReviewsControllerTests
{
    [Fact]
    public async Task List_Sends_Query()
    {
        var sender = new RecordingSender { NextResult = Result<ReviewListDto>.Success(new ReviewListDto(1, 20, [])) };
        var controller = BuildController(sender);
        var companyId = Guid.NewGuid();

        var result = await controller.List(companyId, 1, 20, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<GetCompanyReviewsQuery>(sender.LastRequest);
    }

    [Fact]
    public async Task Create_Sends_Command()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<ReviewDto>.Success(new ReviewDto(1, "company", null, Guid.NewGuid(), Guid.NewGuid(), "u", null, 5, null, "c", true, 1, DateTime.UtcNow, DateTime.UtcNow, null))
        };
        var controller = BuildController(sender);
        var companyId = Guid.NewGuid();

        var result = await controller.Create(companyId, new UpsertCompanyReviewRequest(4.5m, "c"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<CreateCompanyReviewCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task Update_And_Delete_Send_Route_Scoped_Commands()
    {
        var sender = new RecordingSender();
        var controller = BuildController(sender);
        var companyId = Guid.NewGuid();

        sender.NextResult = Result<ReviewDto>.Success(new ReviewDto(1, "company", null, companyId, Guid.NewGuid(), "u", null, 4.5m, null, "c", true, 1, DateTime.UtcNow, DateTime.UtcNow, null));
        var updateResult = await controller.Update(companyId, 33, new UpsertCompanyReviewRequest(4.5m, "c"), CancellationToken.None);
        Assert.IsType<OkObjectResult>(updateResult);
        var updateCommand = Assert.IsType<UpdateOwnCompanyReviewCommand>(sender.LastRequest);
        Assert.Equal(companyId, updateCommand.CompanyId);

        sender.NextResult = Result.Success();
        var deleteResult = await controller.Delete(companyId, 33, CancellationToken.None);
        Assert.IsType<OkResult>(deleteResult);
        var deleteCommand = Assert.IsType<DeleteOwnCompanyReviewCommand>(sender.LastRequest);
        Assert.Equal(companyId, deleteCommand.CompanyId);
    }

    private static CompanyReviewsController BuildController(ISender sender)
    {
        var identity = new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "test");
        return new CompanyReviewsController(sender)
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

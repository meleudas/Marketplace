using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Reviews.Commands.ModerateCompanyReview;
using Marketplace.Application.Reviews.Commands.ModerateProductReview;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Reviews.Enums;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Tests;

[Trait("Suite", "API")]
[Trait("Suite", "Reviews")]
public sealed class ApiAdminReviewsControllerTests
{
    [Fact]
    public async Task ModerateProduct_Returns_Unauthorized_When_Sub_Missing()
    {
        var controller = new AdminReviewsController(new RecordingSender())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.ModerateProduct(10, new ModerateProductReviewRequest(ReviewModerationStatus.Hidden), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task ModerateProduct_And_Company_Send_Commands()
    {
        var sender = new RecordingSender();
        var controller = BuildController(sender);

        sender.NextResult = Result<Marketplace.Application.Reviews.DTOs.ReviewDto>.Success(
            new Marketplace.Application.Reviews.DTOs.ReviewDto(1, "product", 10, null, Guid.NewGuid(), "u", 5, null, "t", "c", true, 1, DateTime.UtcNow, DateTime.UtcNow, null));
        var productResult = await controller.ModerateProduct(10, new ModerateProductReviewRequest(ReviewModerationStatus.Approved), CancellationToken.None);
        Assert.IsType<OkObjectResult>(productResult);
        Assert.IsType<ModerateProductReviewCommand>(sender.LastRequest);

        sender.NextResult = Result<Marketplace.Application.Reviews.DTOs.ReviewDto>.Success(
            new Marketplace.Application.Reviews.DTOs.ReviewDto(2, "company", null, Guid.NewGuid(), Guid.NewGuid(), "u", null, 4.5m, null, "c", true, 1, DateTime.UtcNow, DateTime.UtcNow, null));
        var companyResult = await controller.ModerateCompany(22, new ModerateCompanyReviewRequest(CompanyReviewStatus.Hidden), CancellationToken.None);
        Assert.IsType<OkObjectResult>(companyResult);
        Assert.IsType<ModerateCompanyReviewCommand>(sender.LastRequest);
    }

    private static AdminReviewsController BuildController(ISender sender)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Moderator")
        ], "test");
        return new AdminReviewsController(sender)
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

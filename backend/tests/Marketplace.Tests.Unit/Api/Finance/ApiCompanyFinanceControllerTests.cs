using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Finance.Commands.UpdateCompanyPayoutProfile;
using Marketplace.Application.Finance.DTOs;
using Marketplace.Application.Finance.Queries.GetSellerEarningsSummary;
using Marketplace.Application.Finance.Queries.ListCompanySettlements;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Tests;

[Trait("Suite", "API")]
[Trait("Suite", "Finance")]
public sealed class ApiCompanyFinanceControllerTests
{
    [Fact]
    public async Task GetEarningsSummary_Sends_Query()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<SellerEarningsSummaryDto>.Success(
                new SellerEarningsSummaryDto(Guid.NewGuid(), 0, 100, 0, 10, "UAH"))
        };
        var controller = BuildController(sender);

        var result = await controller.GetEarningsSummary(Guid.NewGuid(), null, null, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<GetSellerEarningsSummaryQuery>(sender.LastRequest);
    }

    [Fact]
    public async Task ListSettlements_Sends_Query()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<IReadOnlyList<SettlementBatchDto>>.Success([])
        };
        var controller = BuildController(sender);

        var result = await controller.ListSettlements(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<ListCompanySettlementsQuery>(sender.LastRequest);
    }

    [Fact]
    public async Task UpdatePayoutProfile_Sends_Command()
    {
        var sender = new RecordingSender { NextResult = Result<CompanyPayoutProfileDto>.Success(new CompanyPayoutProfileDto(Guid.NewGuid(), null, null, null)) };
        var controller = BuildController(sender);

        _ = await controller.UpdatePayoutProfile(
            Guid.NewGuid(),
            new UpdatePayoutProfileRequest("UA123", "Test LLC", null),
            CancellationToken.None);

        Assert.IsType<UpdateCompanyPayoutProfileCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task GetEarningsSummary_Returns_Unauthorized_Without_User()
    {
        var controller = new CompanyFinanceController(new RecordingSender())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.GetEarningsSummary(Guid.NewGuid(), null, null, CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(result);
    }

    private static CompanyFinanceController BuildController(ISender sender)
    {
        var identity = new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "test");
        return new CompanyFinanceController(sender)
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

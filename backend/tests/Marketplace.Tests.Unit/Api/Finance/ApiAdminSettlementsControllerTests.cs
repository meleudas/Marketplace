using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Finance.Commands.ApproveSettlementPayout;
using Marketplace.Application.Finance.Commands.MarkSettlementPaid;
using Marketplace.Application.Finance.DTOs;
using Marketplace.Application.Finance.Queries.ListAdminCommissionRates;
using Marketplace.Application.Finance.Queries.ListAdminSettlements;
using Marketplace.Domain.Finance.Enums;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Tests;

[Trait("Suite", "API")]
[Trait("Suite", "Finance")]
public sealed class ApiAdminSettlementsControllerTests
{
    [Fact]
    public async Task ListSettlements_Sends_Query()
    {
        var sender = new RecordingSender { NextResult = Result<IReadOnlyList<SettlementBatchDto>>.Success([]) };
        var controller = BuildController(sender);

        var result = await controller.ListSettlements(SettlementBatchStatus.Ready, null, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<ListAdminSettlementsQuery>(sender.LastRequest);
    }

    [Fact]
    public async Task ListCommissionRates_Sends_Query()
    {
        var sender = new RecordingSender { NextResult = Result<IReadOnlyList<CompanyCommissionRateHistoryDto>>.Success([]) };
        var controller = BuildController(sender);

        var result = await controller.ListCommissionRates(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<ListAdminCommissionRatesQuery>(sender.LastRequest);
    }

    [Fact]
    public async Task ApprovePayout_Sends_Command()
    {
        var sender = new RecordingSender { NextResult = Result.Success() };
        var controller = BuildController(sender);

        var result = await controller.ApprovePayout(1, CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.IsType<ApproveSettlementPayoutCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task MarkPaid_Sends_Command()
    {
        var sender = new RecordingSender { NextResult = Result.Success() };
        var controller = BuildController(sender);

        var result = await controller.MarkPaid(1, new MarkSettlementPaidRequest("BANK-REF"), CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.IsType<MarkSettlementPaidCommand>(sender.LastRequest);
    }

    private static AdminSettlementsController BuildController(ISender sender)
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, "Admin")], "test");
        return new AdminSettlementsController(sender)
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

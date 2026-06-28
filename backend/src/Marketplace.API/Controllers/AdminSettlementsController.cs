using Marketplace.API.Extensions;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Finance.Commands.ApproveSettlementPayout;
using Marketplace.Application.Finance.Commands.MarkSettlementPaid;
using Marketplace.Application.Finance.Queries.ListAdminCommissionRates;
using Marketplace.Application.Finance.Queries.ListAdminSettlements;
using Marketplace.Domain.Finance.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("AdminSettlements")]
[Route("admin")]
[Authorize(Roles = "Admin")]
public sealed class AdminSettlementsController : ControllerBase
{
    private readonly ISender _sender;

    public AdminSettlementsController(ISender sender) => _sender = sender;

    [HttpGet("settlements")]
    public async Task<IActionResult> ListSettlements(
        [FromQuery] SettlementBatchStatus? status,
        [FromQuery] Guid? companyId,
        CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.CompanyLatencyMs,
            new KeyValuePair<string, object?>("operation", "admin_settlements_list"));
        var result = await _sender.Send(new ListAdminSettlementsQuery(status, companyId), ct);
        return result.ToActionResult();
    }

    [HttpGet("companies/{companyId:guid}/commission-rates")]
    public async Task<IActionResult> ListCommissionRates(Guid companyId, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.CompanyLatencyMs,
            new KeyValuePair<string, object?>("operation", "admin_commission_rates_list"));
        var result = await _sender.Send(new ListAdminCommissionRatesQuery(companyId), ct);
        return result.ToActionResult();
    }

    [HttpPost("settlements/{batchId:long}/approve-payout")]
    public async Task<IActionResult> ApprovePayout(long batchId, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.CompanyLatencyMs,
            new KeyValuePair<string, object?>("operation", "admin_settlement_approve_payout"));
        var result = await _sender.Send(new ApproveSettlementPayoutCommand(batchId), ct);
        return result.ToActionResult();
    }

    [HttpPost("settlements/{batchId:long}/mark-paid")]
    public async Task<IActionResult> MarkPaid(long batchId, [FromBody] MarkSettlementPaidRequest body, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.CompanyLatencyMs,
            new KeyValuePair<string, object?>("operation", "admin_settlement_mark_paid"));
        var result = await _sender.Send(new MarkSettlementPaidCommand(batchId, body.BankReference), ct);
        return result.ToActionResult();
    }
}

public sealed record MarkSettlementPaidRequest(string BankReference);

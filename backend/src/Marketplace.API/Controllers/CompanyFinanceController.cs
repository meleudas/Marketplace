using Marketplace.API.Extensions;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Finance.Commands.UpdateCompanyPayoutProfile;
using Marketplace.Application.Finance.Queries.GetSellerEarningsSummary;
using Marketplace.Application.Finance.Queries.ListCompanySettlements;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("CompanyFinance")]
[Authorize]
public sealed class CompanyFinanceController : ControllerBase
{
    private readonly ISender _sender;

    public CompanyFinanceController(ISender sender) => _sender = sender;

    [HttpGet("companies/{companyId:guid}/earnings/summary")]
    public async Task<IActionResult> GetEarningsSummary(
        Guid companyId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.CompanyLatencyMs,
            new KeyValuePair<string, object?>("operation", "company_earnings_summary"));
        var result = await _sender.Send(
            new GetSellerEarningsSummaryQuery(companyId, actorId, User.IsInRole("Admin"), from, to),
            ct);
        return result.ToActionResult();
    }

    [HttpGet("companies/{companyId:guid}/settlements")]
    public async Task<IActionResult> ListSettlements(Guid companyId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.CompanyLatencyMs,
            new KeyValuePair<string, object?>("operation", "company_settlements_list"));
        var result = await _sender.Send(
            new ListCompanySettlementsQuery(companyId, actorId, User.IsInRole("Admin")),
            ct);
        return result.ToActionResult();
    }

    [HttpPatch("companies/{companyId:guid}/payout-profile")]
    public async Task<IActionResult> UpdatePayoutProfile(
        Guid companyId,
        [FromBody] UpdatePayoutProfileRequest body,
        CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.CompanyLatencyMs,
            new KeyValuePair<string, object?>("operation", "company_payout_profile_update"));
        var result = await _sender.Send(
            new UpdateCompanyPayoutProfileCommand(
                companyId,
                actorId,
                User.IsInRole("Admin"),
                body.PayoutIban,
                body.PayoutRecipientName,
                body.PayoutProviderAccountId),
            ct);
        return result.ToActionResult();
    }
}

public sealed record UpdatePayoutProfileRequest(
    string? PayoutIban,
    string? PayoutRecipientName,
    string? PayoutProviderAccountId);

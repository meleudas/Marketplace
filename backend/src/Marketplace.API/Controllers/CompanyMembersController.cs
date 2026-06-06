using Marketplace.API.Extensions;
using Marketplace.Application.Companies.Commands.AssignCompanyMemberRole;
using Marketplace.Application.Companies.Commands.ChangeCompanyMemberRole;
using Marketplace.Application.Companies.Commands.RemoveCompanyMember;
using Marketplace.Application.Companies.Queries.GetCompanyMembers;
using Marketplace.Application.Companies.Queries.GetMyCompanyRole;
using Marketplace.Application.Common.Observability;
using Marketplace.Domain.Companies.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("companies/{companyId:guid}/members")]
[Authorize]
public sealed class CompanyMembersController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<CompanyMembersController> _logger;

    public CompanyMembersController(ISender sender, ILogger<CompanyMembersController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetMembers(Guid companyId, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CompanyLatencyMs, new KeyValuePair<string, object?>("operation", "company_members_list"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.CompanyErrors.Add(1, [new KeyValuePair<string, object?>("operation", "company_members_list"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }

        var result = await _sender.Send(new GetCompanyMembersQuery(companyId, actorId, User.IsInRole("Admin")), ct);
        RecordCompanyResult("company_members_list", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyRole(Guid companyId, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CompanyLatencyMs, new KeyValuePair<string, object?>("operation", "company_member_me"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.CompanyErrors.Add(1, [new KeyValuePair<string, object?>("operation", "company_member_me"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }

        var result = await _sender.Send(new GetMyCompanyRoleQuery(companyId, actorId), ct);
        RecordCompanyResult("company_member_me", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpPost("{userId:guid}/role")]
    public async Task<IActionResult> AssignRole(Guid companyId, Guid userId, [FromBody] CompanyMemberRoleRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CompanyLatencyMs, new KeyValuePair<string, object?>("operation", "company_member_assign_role"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.CompanyErrors.Add(1, [new KeyValuePair<string, object?>("operation", "company_member_assign_role"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }

        if (!Enum.TryParse<CompanyMembershipRole>(request.Role, true, out var role))
        {
            MarketplaceMetrics.CompanyErrors.Add(1, [new KeyValuePair<string, object?>("operation", "company_member_assign_role"), new KeyValuePair<string, object?>("reason", "invalid_role")]);
            return BadRequest(new ProblemDetails { Title = "Invalid role", Detail = "Role is invalid", Status = StatusCodes.Status400BadRequest });
        }

        var command = new AssignCompanyMemberRoleCommand(companyId, userId, role, actorId, User.IsInRole("Admin"));
        var result = await _sender.Send(command, ct);
        RecordCompanyResult("company_member_assign_role", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpPatch("{userId:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid companyId, Guid userId, [FromBody] CompanyMemberRoleRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CompanyLatencyMs, new KeyValuePair<string, object?>("operation", "company_member_change_role"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.CompanyErrors.Add(1, [new KeyValuePair<string, object?>("operation", "company_member_change_role"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }

        if (!Enum.TryParse<CompanyMembershipRole>(request.Role, true, out var role))
        {
            MarketplaceMetrics.CompanyErrors.Add(1, [new KeyValuePair<string, object?>("operation", "company_member_change_role"), new KeyValuePair<string, object?>("reason", "invalid_role")]);
            return BadRequest(new ProblemDetails { Title = "Invalid role", Detail = "Role is invalid", Status = StatusCodes.Status400BadRequest });
        }

        var command = new ChangeCompanyMemberRoleCommand(companyId, userId, role, actorId, User.IsInRole("Admin"));
        var result = await _sender.Send(command, ct);
        RecordCompanyResult("company_member_change_role", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid companyId, Guid userId, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CompanyLatencyMs, new KeyValuePair<string, object?>("operation", "company_member_remove"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.CompanyErrors.Add(1, [new KeyValuePair<string, object?>("operation", "company_member_remove"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }

        var command = new RemoveCompanyMemberCommand(companyId, userId, actorId, User.IsInRole("Admin"));
        var result = await _sender.Send(command, ct);
        RecordCompanyResult("company_member_remove", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    private void RecordCompanyResult(string operation, bool success, string? error)
    {
        if (success)
        {
            MarketplaceMetrics.CompanyOps.Add(1, [new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("status", "success")]);
            return;
        }

        MarketplaceMetrics.CompanyErrors.Add(1, [new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("reason", "application_failure")]);
        _logger.LogWarning("Company member operation {Operation} failed: {Error}", operation, error);
    }
}

public sealed record CompanyMemberRoleRequest(string Role);

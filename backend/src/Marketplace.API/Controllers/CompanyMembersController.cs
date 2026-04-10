using Marketplace.API.Extensions;
using Marketplace.Application.Companies.Commands.AssignCompanyMemberRole;
using Marketplace.Application.Companies.Commands.ChangeCompanyMemberRole;
using Marketplace.Application.Companies.Commands.RemoveCompanyMember;
using Marketplace.Application.Companies.Queries.GetCompanyMembers;
using Marketplace.Application.Companies.Queries.GetMyCompanyRole;
using Marketplace.Domain.Companies.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("companies/{companyId:guid}/members")]
[Authorize]
public sealed class CompanyMembersController : ControllerBase
{
    private readonly ISender _sender;

    public CompanyMembersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetMembers(Guid companyId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        var result = await _sender.Send(new GetCompanyMembersQuery(companyId, actorId, User.IsInRole("Admin")), ct);
        return result.ToActionResult();
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyRole(Guid companyId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        var result = await _sender.Send(new GetMyCompanyRoleQuery(companyId, actorId), ct);
        return result.ToActionResult();
    }

    [HttpPost("{userId:guid}/role")]
    public async Task<IActionResult> AssignRole(Guid companyId, Guid userId, [FromBody] CompanyMemberRoleRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        if (!Enum.TryParse<CompanyMembershipRole>(request.Role, true, out var role))
            return BadRequest(new ProblemDetails { Title = "Invalid role", Detail = "Role is invalid", Status = StatusCodes.Status400BadRequest });

        var command = new AssignCompanyMemberRoleCommand(companyId, userId, role, actorId, User.IsInRole("Admin"));
        var result = await _sender.Send(command, ct);
        return result.ToActionResult();
    }

    [HttpPatch("{userId:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid companyId, Guid userId, [FromBody] CompanyMemberRoleRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        if (!Enum.TryParse<CompanyMembershipRole>(request.Role, true, out var role))
            return BadRequest(new ProblemDetails { Title = "Invalid role", Detail = "Role is invalid", Status = StatusCodes.Status400BadRequest });

        var command = new ChangeCompanyMemberRoleCommand(companyId, userId, role, actorId, User.IsInRole("Admin"));
        var result = await _sender.Send(command, ct);
        return result.ToActionResult();
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid companyId, Guid userId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        var command = new RemoveCompanyMemberCommand(companyId, userId, actorId, User.IsInRole("Admin"));
        var result = await _sender.Send(command, ct);
        return result.ToActionResult();
    }
}

public sealed record CompanyMemberRoleRequest(string Role);

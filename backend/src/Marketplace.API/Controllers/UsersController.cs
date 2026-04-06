using Marketplace.API.Extensions;
using Marketplace.Application.Users.Commands.AssignUserRole;
using Marketplace.Application.Users.Services;
using Marketplace.Domain.Users.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserReadService _userReadService;
    private readonly IUserManagementService _userManagementService;
    private readonly ISender _sender;

    public UsersController(
        IUserReadService userReadService,
        IUserManagementService userManagementService,
        ISender sender)
    {
        _userReadService = userReadService;
        _userManagementService = userManagementService;
        _sender = sender;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        if (!User.TryGetUserId(out var identityUserId))
            return Unauthorized();

        var result = await _userReadService.GetMeAsync(identityUserId, ct);
        return result.ToActionResult();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _userReadService.GetAllAsync(ct);
        return result.ToActionResult();
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchByUserName([FromQuery] string userName, CancellationToken ct)
    {
        var result = await _userReadService.SearchByUserNameAsync(userName, ct);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var currentId))
            return Unauthorized();

        if (currentId != id)
            return Forbid();

        var result = await _userManagementService.DeleteAccountAsync(id, ct);
        return result.ToActionResult();
    }

    [HttpPatch("{id:guid}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignUserRoleRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            return BadRequest(new ProblemDetails
            {
                Title = "Помилка запиту",
                Detail = "Role is invalid",
                Status = StatusCodes.Status400BadRequest
            });

        var result = await _sender.Send(new AssignUserRoleCommand(id, role), ct);
        return result.ToActionResult();
    }
}

public sealed record AssignUserRoleRequest(string Role);

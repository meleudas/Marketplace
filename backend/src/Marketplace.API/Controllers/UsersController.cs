using Marketplace.API.Extensions;
using Marketplace.Application.Users.Services;
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

    public UsersController(
        IUserReadService userReadService,
        IUserManagementService userManagementService)
    {
        _userReadService = userReadService;
        _userManagementService = userManagementService;
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
}

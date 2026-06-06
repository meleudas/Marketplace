using Marketplace.Application.Common.Ports;
using Marketplace.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("admin/outbox")]
[Authorize(Roles = "Admin")]
public sealed class AdminOutboxController : ControllerBase
{
    private readonly IOutboxWriter _outbox;

    public AdminOutboxController(IOutboxWriter outbox)
    {
        _outbox = outbox;
    }

    [HttpPost("{messageId:guid}/requeue")]
    public async Task<IActionResult> Requeue(Guid messageId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out _))
            return Unauthorized();
        if (!User.IsInRole("Admin"))
            return Forbid();

        await _outbox.RequeueDeadLetterAsync(messageId, ct);
        return Ok();
    }
}

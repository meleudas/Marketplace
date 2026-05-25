using Marketplace.API.Extensions;
using Marketplace.Application.Products.Commands.ApproveProduct;
using Marketplace.Application.Products.Commands.RejectProduct;
using Marketplace.Application.Products.Queries.GetPendingProducts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("admin/products")]
[Authorize(Roles = "Admin,Moderator")]
public sealed class AdminProductsController : ControllerBase
{
    private readonly ISender _sender;

    public AdminProductsController(ISender sender) => _sender = sender;

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        var result = await _sender.Send(new GetPendingProductsQuery(), ct);
        return result.ToActionResult();
    }

    [HttpPost("{id:long}/approve")]
    public async Task<IActionResult> Approve(long id, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new ApproveProductCommand(id, actorId), ct);
        return result.ToActionResult();
    }

    [HttpPost("{id:long}/reject")]
    public async Task<IActionResult> Reject(long id, [FromBody] RejectProductBody? body, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new RejectProductCommand(id, actorId, body?.Reason), ct);
        return result.ToActionResult();
    }
}

public sealed record RejectProductBody(string? Reason);

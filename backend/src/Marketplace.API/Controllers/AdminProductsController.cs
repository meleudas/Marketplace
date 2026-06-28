using Marketplace.API.Extensions;
using Marketplace.Application.Products.Commands.ApproveProduct;
using Marketplace.Application.Products.Commands.RejectProduct;
using Marketplace.Application.Products.Queries.GetPendingProducts;
using Marketplace.Application.Common.Observability;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("AdminProducts")]
[Route("admin/products")]
[Authorize(Roles = "Admin,Moderator")]
public sealed class AdminProductsController : ControllerBase
{
    private readonly ISender _sender;

    public AdminProductsController(ISender sender) => _sender = sender;

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ProductLatencyMs, new KeyValuePair<string, object?>("operation", "admin_products_pending"));
        var result = await _sender.Send(new GetPendingProductsQuery(), ct);
        TrackProductResult("admin_products_pending", result);
        return result.ToActionResult();
    }

    [HttpPost("{id:long}/approve")]
    public async Task<IActionResult> Approve(long id, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ProductLatencyMs, new KeyValuePair<string, object?>("operation", "admin_products_approve"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.ProductErrors.Add(1, [new KeyValuePair<string, object?>("operation", "admin_products_approve"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }
        var result = await _sender.Send(new ApproveProductCommand(id, actorId), ct);
        TrackProductResult("admin_products_approve", result);
        return result.ToActionResult();
    }

    [HttpPost("{id:long}/reject")]
    public async Task<IActionResult> Reject(long id, [FromBody] RejectProductBody? body, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.ProductLatencyMs, new KeyValuePair<string, object?>("operation", "admin_products_reject"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.ProductErrors.Add(1, [new KeyValuePair<string, object?>("operation", "admin_products_reject"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }
        var result = await _sender.Send(new RejectProductCommand(id, actorId, body?.Reason), ct);
        TrackProductResult("admin_products_reject", result);
        return result.ToActionResult();
    }

    private static void TrackProductResult(string operation, Marketplace.Domain.Shared.Kernel.Result result)
    {
        if (result.IsSuccess)
        {
            MarketplaceMetrics.ProductOps.Add(1, [new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("status", "success")]);
            return;
        }

        var reason = string.Equals(result.Error, "Product not found", StringComparison.OrdinalIgnoreCase)
            ? "not_found"
            : "application_failure";
        MarketplaceMetrics.ProductErrors.Add(1, [new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("reason", reason)]);
    }

    private static void TrackProductResult<T>(string operation, Marketplace.Domain.Shared.Kernel.Result<T> result)
    {
        if (result.IsSuccess)
        {
            MarketplaceMetrics.ProductOps.Add(1, [new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("status", "success")]);
            return;
        }

        MarketplaceMetrics.ProductErrors.Add(1, [new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("reason", "application_failure")]);
    }
}

public sealed record RejectProductBody(string? Reason);

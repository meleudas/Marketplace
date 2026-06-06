using Marketplace.API.Extensions;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Orders.Commands.CancelOrder;
using Marketplace.Application.Orders.Commands.UpdateOrderStatus;
using Marketplace.Application.Orders.Queries.GetOrderById;
using Marketplace.Application.Orders.Queries.ListOrders;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Application.Common.Observability;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Authorize]
public sealed class OrdersController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IHttpIdempotencyStore _idempotency;

    public OrdersController(ISender sender, IHttpIdempotencyStore idempotency)
    {
        _sender = sender;
        _idempotency = idempotency;
    }

    [HttpGet("me/orders")]
    public async Task<IActionResult> ListMy(
        [FromQuery] string[]? statuses,
        [FromQuery] DateTime? createdFromUtc,
        [FromQuery] DateTime? createdToUtc,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.OrderLatencyMs, new KeyValuePair<string, object?>("operation", "orders_list_my"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.OrderErrors.Add(1, [new KeyValuePair<string, object?>("operation", "orders_list_my"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }

        var result = await _sender.Send(new ListOrdersQuery(
            OrderListScope.My,
            actorId,
            User.IsInRole("Admin"),
            null,
            ParseStatuses(statuses),
            createdFromUtc,
            createdToUtc,
            search,
            sort,
            page,
            pageSize), ct);
        TrackOrderResult("orders_list_my", result);
        return result.ToActionResult();
    }

    [HttpGet("me/orders/{orderId:long}")]
    public async Task<IActionResult> GetMy(long orderId, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.OrderLatencyMs, new KeyValuePair<string, object?>("operation", "orders_get_my"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.OrderErrors.Add(1, [new KeyValuePair<string, object?>("operation", "orders_get_my"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }
        var result = await _sender.Send(new GetOrderByIdQuery(orderId, actorId, User.IsInRole("Admin")), ct);
        TrackOrderResult("orders_get_my", result);
        return result.ToActionResult();
    }

    [HttpGet("companies/{companyId:guid}/orders")]
    public async Task<IActionResult> ListCompany(
        Guid companyId,
        [FromQuery] string[]? statuses,
        [FromQuery] DateTime? createdFromUtc,
        [FromQuery] DateTime? createdToUtc,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.OrderLatencyMs, new KeyValuePair<string, object?>("operation", "orders_list_company"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.OrderErrors.Add(1, [new KeyValuePair<string, object?>("operation", "orders_list_company"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }

        var result = await _sender.Send(new ListOrdersQuery(
            OrderListScope.Company,
            actorId,
            User.IsInRole("Admin"),
            companyId,
            ParseStatuses(statuses),
            createdFromUtc,
            createdToUtc,
            search,
            sort,
            page,
            pageSize), ct);
        TrackOrderResult("orders_list_company", result);
        return result.ToActionResult();
    }

    [HttpGet("companies/{companyId:guid}/orders/{orderId:long}")]
    public async Task<IActionResult> GetCompany(Guid companyId, long orderId, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.OrderLatencyMs, new KeyValuePair<string, object?>("operation", "orders_get_company"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.OrderErrors.Add(1, [new KeyValuePair<string, object?>("operation", "orders_get_company"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }

        var result = await _sender.Send(new GetOrderByIdQuery(orderId, actorId, User.IsInRole("Admin")), ct);
        if (!result.IsSuccess || result.Value is null || result.Value.CompanyId != companyId)
        {
            TrackOrderResult("orders_get_company", result);
            return result.ToActionResult();
        }
        TrackOrderResult("orders_get_company", result);
        return result.ToActionResult();
    }

    [HttpGet("admin/orders")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ListAdmin(
        [FromQuery] string[]? statuses,
        [FromQuery] Guid? companyId,
        [FromQuery] DateTime? createdFromUtc,
        [FromQuery] DateTime? createdToUtc,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.OrderLatencyMs, new KeyValuePair<string, object?>("operation", "orders_list_admin"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.OrderErrors.Add(1, [new KeyValuePair<string, object?>("operation", "orders_list_admin"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }
        var result = await _sender.Send(new ListOrdersQuery(
            OrderListScope.Admin,
            actorId,
            true,
            companyId,
            ParseStatuses(statuses),
            createdFromUtc,
            createdToUtc,
            search,
            sort,
            page,
            pageSize), ct);
        TrackOrderResult("orders_list_admin", result);
        return result.ToActionResult();
    }

    [HttpGet("admin/orders/{orderId:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdmin(long orderId, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.OrderLatencyMs, new KeyValuePair<string, object?>("operation", "orders_get_admin"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.OrderErrors.Add(1, [new KeyValuePair<string, object?>("operation", "orders_get_admin"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }
        var result = await _sender.Send(new GetOrderByIdQuery(orderId, actorId, true), ct);
        TrackOrderResult("orders_get_admin", result);
        return result.ToActionResult();
    }

    [HttpPost("orders/{orderId:long}/status")]
    public async Task<IActionResult> UpdateStatus(long orderId, [FromBody] UpdateOrderStatusRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.OrderLatencyMs, new KeyValuePair<string, object?>("operation", "orders_update_status"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.OrderErrors.Add(1, [new KeyValuePair<string, object?>("operation", "orders_update_status"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }
        if (!Request.TryGetIdempotencyKey(out var idempotencyKey))
        {
            MarketplaceMetrics.OrderErrors.Add(1, [new KeyValuePair<string, object?>("operation", "orders_update_status"), new KeyValuePair<string, object?>("reason", "idempotency_key_missing")]);
            return BadRequest("Idempotency-Key header is required.");
        }
        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var status))
        {
            MarketplaceMetrics.OrderErrors.Add(1, [new KeyValuePair<string, object?>("operation", "orders_update_status"), new KeyValuePair<string, object?>("reason", "invalid_status")]);
            return BadRequest("Invalid status");
        }

        var scope = $"order-status:{orderId}:{actorId:N}";
        var requestHash = HttpIdempotencyExtensions.BuildRequestHash(orderId.ToString(), actorId.ToString("N"), request.Status, request.TrackingNumber);
        var begin = await _idempotency.TryBeginAsync(scope, idempotencyKey, requestHash, TimeSpan.FromHours(12), ct);
        if (begin.State == HttpIdempotencyBeginState.Completed && begin.StoredResponse is not null)
            return this.ReplayResponse(begin.StoredResponse);
        if (begin.State == HttpIdempotencyBeginState.InProgress)
        {
            MarketplaceMetrics.OrderErrors.Add(1, [new KeyValuePair<string, object?>("operation", "orders_update_status"), new KeyValuePair<string, object?>("reason", "idempotency_in_progress")]);
            return Conflict("Request with this Idempotency-Key is already in progress.");
        }
        if (begin.State == HttpIdempotencyBeginState.RequestMismatch)
        {
            MarketplaceMetrics.OrderErrors.Add(1, [new KeyValuePair<string, object?>("operation", "orders_update_status"), new KeyValuePair<string, object?>("reason", "idempotency_request_mismatch")]);
            return Conflict("Idempotency-Key already used with different request payload.");
        }

        var result = await _sender.Send(new UpdateOrderStatusCommand(orderId, actorId, User.IsInRole("Admin"), status, request.TrackingNumber, idempotencyKey), ct);
        var actionResult = result.ToActionResult();
        var snapshot = actionResult.SnapshotResult();
        await _idempotency.CompleteAsync(scope, idempotencyKey, requestHash, snapshot.StatusCode, snapshot.BodyJson, ct);
        TrackOrderResult("orders_update_status", result);
        return actionResult;
    }

    [HttpPost("orders/{orderId:long}/cancel")]
    public async Task<IActionResult> Cancel(long orderId, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.OrderLatencyMs, new KeyValuePair<string, object?>("operation", "orders_cancel"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.OrderErrors.Add(1, [new KeyValuePair<string, object?>("operation", "orders_cancel"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }
        if (!Request.TryGetIdempotencyKey(out var idempotencyKey))
        {
            MarketplaceMetrics.OrderErrors.Add(1, [new KeyValuePair<string, object?>("operation", "orders_cancel"), new KeyValuePair<string, object?>("reason", "idempotency_key_missing")]);
            return BadRequest("Idempotency-Key header is required.");
        }

        var scope = $"order-cancel:{orderId}:{actorId:N}";
        var requestHash = HttpIdempotencyExtensions.BuildRequestHash(orderId.ToString(), actorId.ToString("N"));
        var begin = await _idempotency.TryBeginAsync(scope, idempotencyKey, requestHash, TimeSpan.FromHours(12), ct);
        if (begin.State == HttpIdempotencyBeginState.Completed && begin.StoredResponse is not null)
            return this.ReplayResponse(begin.StoredResponse);
        if (begin.State == HttpIdempotencyBeginState.InProgress)
        {
            MarketplaceMetrics.OrderErrors.Add(1, [new KeyValuePair<string, object?>("operation", "orders_cancel"), new KeyValuePair<string, object?>("reason", "idempotency_in_progress")]);
            return Conflict("Request with this Idempotency-Key is already in progress.");
        }
        if (begin.State == HttpIdempotencyBeginState.RequestMismatch)
        {
            MarketplaceMetrics.OrderErrors.Add(1, [new KeyValuePair<string, object?>("operation", "orders_cancel"), new KeyValuePair<string, object?>("reason", "idempotency_request_mismatch")]);
            return Conflict("Idempotency-Key already used with different request payload.");
        }

        var result = await _sender.Send(new CancelOrderCommand(orderId, actorId, User.IsInRole("Admin"), idempotencyKey), ct);
        var actionResult = result.ToActionResult();
        var snapshot = actionResult.SnapshotResult();
        await _idempotency.CompleteAsync(scope, idempotencyKey, requestHash, snapshot.StatusCode, snapshot.BodyJson, ct);
        TrackOrderResult("orders_cancel", result);
        return actionResult;
    }

    private static IReadOnlyList<OrderStatus>? ParseStatuses(string[]? statuses)
    {
        if (statuses is null || statuses.Length == 0)
            return null;
        return statuses
            .Select(x => Enum.TryParse<OrderStatus>(x, true, out var v) ? v : (OrderStatus?)null)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();
    }

    private static void TrackOrderResult(string operation, Marketplace.Domain.Shared.Kernel.Result result)
    {
        if (result.IsSuccess)
        {
            MarketplaceMetrics.OrderOps.Add(1, [new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("status", "success")]);
            return;
        }

        MarketplaceMetrics.OrderErrors.Add(1, [new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("reason", "application_failure")]);
    }

    private static void TrackOrderResult<T>(string operation, Marketplace.Domain.Shared.Kernel.Result<T> result)
    {
        if (result.IsSuccess)
        {
            MarketplaceMetrics.OrderOps.Add(1, [new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("status", "success")]);
            return;
        }

        var reason = string.Equals(result.Error, "Forbidden", StringComparison.OrdinalIgnoreCase)
            ? "forbidden"
            : "application_failure";
        MarketplaceMetrics.OrderErrors.Add(1, [new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("reason", reason)]);
    }
}

public sealed record UpdateOrderStatusRequest(string Status, string? TrackingNumber);

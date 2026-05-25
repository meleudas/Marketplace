using Marketplace.API.Extensions;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Orders.Commands.CancelOrder;
using Marketplace.Application.Orders.Commands.UpdateOrderStatus;
using Marketplace.Application.Orders.Queries.GetOrderById;
using Marketplace.Application.Orders.Queries.ListOrders;
using Marketplace.Domain.Orders.Enums;
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
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

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
        return result.ToActionResult();
    }

    [HttpGet("me/orders/{orderId:long}")]
    public async Task<IActionResult> GetMy(long orderId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new GetOrderByIdQuery(orderId, actorId, User.IsInRole("Admin")), ct);
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
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

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
        return result.ToActionResult();
    }

    [HttpGet("companies/{companyId:guid}/orders/{orderId:long}")]
    public async Task<IActionResult> GetCompany(Guid companyId, long orderId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        var result = await _sender.Send(new GetOrderByIdQuery(orderId, actorId, User.IsInRole("Admin")), ct);
        if (!result.IsSuccess || result.Value is null || result.Value.CompanyId != companyId)
            return result.ToActionResult();
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
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
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
        return result.ToActionResult();
    }

    [HttpGet("admin/orders/{orderId:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdmin(long orderId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new GetOrderByIdQuery(orderId, actorId, true), ct);
        return result.ToActionResult();
    }

    [HttpPost("orders/{orderId:long}/status")]
    public async Task<IActionResult> UpdateStatus(long orderId, [FromBody] UpdateOrderStatusRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        if (!Request.TryGetIdempotencyKey(out var idempotencyKey))
            return BadRequest("Idempotency-Key header is required.");
        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var status))
            return BadRequest("Invalid status");

        var scope = $"order-status:{orderId}:{actorId:N}";
        var requestHash = HttpIdempotencyExtensions.BuildRequestHash(orderId.ToString(), actorId.ToString("N"), request.Status, request.TrackingNumber);
        var begin = await _idempotency.TryBeginAsync(scope, idempotencyKey, requestHash, TimeSpan.FromHours(12), ct);
        if (begin.State == HttpIdempotencyBeginState.Completed && begin.StoredResponse is not null)
            return this.ReplayResponse(begin.StoredResponse);
        if (begin.State == HttpIdempotencyBeginState.InProgress)
            return Conflict("Request with this Idempotency-Key is already in progress.");
        if (begin.State == HttpIdempotencyBeginState.RequestMismatch)
            return Conflict("Idempotency-Key already used with different request payload.");

        var result = await _sender.Send(new UpdateOrderStatusCommand(orderId, actorId, User.IsInRole("Admin"), status, request.TrackingNumber, idempotencyKey), ct);
        var actionResult = result.ToActionResult();
        var snapshot = actionResult.SnapshotResult();
        await _idempotency.CompleteAsync(scope, idempotencyKey, requestHash, snapshot.StatusCode, snapshot.BodyJson, ct);
        return actionResult;
    }

    [HttpPost("orders/{orderId:long}/cancel")]
    public async Task<IActionResult> Cancel(long orderId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        if (!Request.TryGetIdempotencyKey(out var idempotencyKey))
            return BadRequest("Idempotency-Key header is required.");

        var scope = $"order-cancel:{orderId}:{actorId:N}";
        var requestHash = HttpIdempotencyExtensions.BuildRequestHash(orderId.ToString(), actorId.ToString("N"));
        var begin = await _idempotency.TryBeginAsync(scope, idempotencyKey, requestHash, TimeSpan.FromHours(12), ct);
        if (begin.State == HttpIdempotencyBeginState.Completed && begin.StoredResponse is not null)
            return this.ReplayResponse(begin.StoredResponse);
        if (begin.State == HttpIdempotencyBeginState.InProgress)
            return Conflict("Request with this Idempotency-Key is already in progress.");
        if (begin.State == HttpIdempotencyBeginState.RequestMismatch)
            return Conflict("Idempotency-Key already used with different request payload.");

        var result = await _sender.Send(new CancelOrderCommand(orderId, actorId, User.IsInRole("Admin"), idempotencyKey), ct);
        var actionResult = result.ToActionResult();
        var snapshot = actionResult.SnapshotResult();
        await _idempotency.CompleteAsync(scope, idempotencyKey, requestHash, snapshot.StatusCode, snapshot.BodyJson, ct);
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
}

public sealed record UpdateOrderStatusRequest(string Status, string? TrackingNumber);

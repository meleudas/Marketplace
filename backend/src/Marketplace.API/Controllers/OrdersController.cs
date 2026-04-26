using Marketplace.API.Extensions;
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

    public OrdersController(ISender sender) => _sender = sender;

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
        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var status))
            return BadRequest("Invalid status");
        var result = await _sender.Send(new UpdateOrderStatusCommand(orderId, actorId, User.IsInRole("Admin"), status, request.TrackingNumber), ct);
        return result.ToActionResult();
    }

    [HttpPost("orders/{orderId:long}/cancel")]
    public async Task<IActionResult> Cancel(long orderId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new CancelOrderCommand(orderId, actorId, User.IsInRole("Admin")), ct);
        return result.ToActionResult();
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

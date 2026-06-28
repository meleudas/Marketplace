using Marketplace.API.Extensions;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Carts.Commands.AddCartItem;
using Marketplace.Application.Carts.Commands.CheckoutCart;
using Marketplace.Application.Carts.Commands.ClearCart;
using Marketplace.Application.Carts.Commands.RemoveCartItem;
using Marketplace.Application.Carts.Commands.UpdateCartItemQuantity;
using Marketplace.Application.Carts.Queries.GetMyCart;
using Marketplace.Application.Common.Observability;
using Marketplace.Domain.Orders.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("Cart")]
[Route("me/cart")]
[Authorize]
public sealed class CartController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IHttpIdempotencyStore _idempotency;
    private readonly ILogger<CartController> _logger;

    public CartController(ISender sender, IHttpIdempotencyStore idempotency, ILogger<CartController> logger)
    {
        _sender = sender;
        _idempotency = idempotency;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyCart(CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new GetMyCartQuery(actorId), ct);
        return result.ToActionResult();
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.CartLatencyMs,
            new KeyValuePair<string, object?>("operation", "add_item"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.CartErrors.Add(1, [new KeyValuePair<string, object?>("operation", "add_item"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }
        var result = await _sender.Send(new AddCartItemCommand(actorId, request.ProductId, request.Quantity), ct);
        RecordCartResult("add_item", actorId, result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpPatch("items/{itemId:long}")]
    public async Task<IActionResult> UpdateItemQuantity(long itemId, [FromBody] UpdateCartItemRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.CartLatencyMs,
            new KeyValuePair<string, object?>("operation", "update_item_quantity"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.CartErrors.Add(1, [new KeyValuePair<string, object?>("operation", "update_item_quantity"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }
        var result = await _sender.Send(new UpdateCartItemQuantityCommand(actorId, itemId, request.Quantity), ct);
        RecordCartResult("update_item_quantity", actorId, result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpDelete("items/{itemId:long}")]
    public async Task<IActionResult> RemoveItem(long itemId, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.CartLatencyMs,
            new KeyValuePair<string, object?>("operation", "remove_item"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.CartErrors.Add(1, [new KeyValuePair<string, object?>("operation", "remove_item"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }
        var result = await _sender.Send(new RemoveCartItemCommand(actorId, itemId), ct);
        RecordCartResult("remove_item", actorId, result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.CartLatencyMs,
            new KeyValuePair<string, object?>("operation", "clear_cart"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.CartErrors.Add(1, [new KeyValuePair<string, object?>("operation", "clear_cart"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }
        var result = await _sender.Send(new ClearCartCommand(actorId), ct);
        RecordCartResult("clear_cart", actorId, result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutCartRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.CheckoutLatencyMs,
            new KeyValuePair<string, object?>("operation", "checkout"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.CheckoutErrors.Add(1, [new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }
        if (!Request.TryGetIdempotencyKey(out var idempotencyKey))
        {
            MarketplaceMetrics.CheckoutErrors.Add(1, [new KeyValuePair<string, object?>("reason", "missing_idempotency_key")]);
            return BadRequest("Idempotency-Key header is required.");
        }

        if (!Enum.TryParse<CheckoutPaymentMethod>(request.PaymentMethod, true, out var paymentMethod))
        {
            MarketplaceMetrics.CheckoutErrors.Add(1, [new KeyValuePair<string, object?>("reason", "invalid_payment_method")]);
            return BadRequest("Invalid paymentMethod");
        }

        var scope = $"checkout:{actorId:N}";
        var requestHash = HttpIdempotencyExtensions.BuildRequestHash(
            actorId.ToString("N"),
            request.PaymentMethod,
            request.ShippingMethodId.ToString(),
            request.Notes,
            request.Address.FirstName,
            request.Address.LastName,
            request.Address.Phone,
            request.Address.Street,
            request.Address.City,
            request.Address.State,
            request.Address.PostalCode,
            request.Address.Country);
        var begin = await _idempotency.TryBeginAsync(scope, idempotencyKey, requestHash, TimeSpan.FromHours(12), ct);
        if (begin.State == HttpIdempotencyBeginState.Completed && begin.StoredResponse is not null)
        {
            MarketplaceMetrics.CheckoutOps.Add(1, [new KeyValuePair<string, object?>("status", "replay")]);
            return this.ReplayResponse(begin.StoredResponse);
        }
        if (begin.State == HttpIdempotencyBeginState.InProgress)
        {
            MarketplaceMetrics.CheckoutErrors.Add(1, [new KeyValuePair<string, object?>("reason", "idempotency_in_progress")]);
            return Conflict("Request with this Idempotency-Key is already in progress.");
        }
        if (begin.State == HttpIdempotencyBeginState.RequestMismatch)
        {
            MarketplaceMetrics.CheckoutErrors.Add(1, [new KeyValuePair<string, object?>("reason", "idempotency_request_mismatch")]);
            return Conflict("Idempotency-Key already used with different request payload.");
        }

        var command = new CheckoutCartCommand(
            actorId,
            paymentMethod,
            request.ShippingMethodId,
            new CheckoutAddressDto(
                request.Address.FirstName,
                request.Address.LastName,
                request.Address.Phone,
                request.Address.Street,
                request.Address.City,
                request.Address.State,
                request.Address.PostalCode,
                request.Address.Country),
            request.Notes,
            idempotencyKey);

        var result = await _sender.Send(command, ct);
        if (result.IsSuccess)
        {
            MarketplaceMetrics.CheckoutOps.Add(1, [new KeyValuePair<string, object?>("status", "success")]);
        }
        else
        {
            MarketplaceMetrics.CheckoutErrors.Add(1, [new KeyValuePair<string, object?>("reason", "application_failure")]);
            _logger.LogWarning("Checkout failed for user {ActorUserId}. Error: {Error}", actorId, result.Error);
        }
        var actionResult = result.ToActionResult();
        var snapshot = actionResult.SnapshotResult();
        await _idempotency.CompleteAsync(scope, idempotencyKey, requestHash, snapshot.StatusCode, snapshot.BodyJson, ct);
        return actionResult;
    }

    private void RecordCartResult(string operation, Guid actorId, bool success, string? error)
    {
        if (success)
        {
            MarketplaceMetrics.CartOps.Add(1, [new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("status", "success")]);
            return;
        }

        MarketplaceMetrics.CartErrors.Add(1, [new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("reason", "application_failure")]);
        _logger.LogWarning("Cart operation {Operation} failed for user {ActorUserId}. Error: {Error}", operation, actorId, error);
    }
}

public sealed record AddCartItemRequest(long ProductId, int Quantity);
public sealed record UpdateCartItemRequest(int Quantity);
public sealed record CheckoutCartRequest(string PaymentMethod, long ShippingMethodId, CheckoutAddressRequest Address, string? Notes);
public sealed record CheckoutAddressRequest(
    string FirstName,
    string LastName,
    string Phone,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country);

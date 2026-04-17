using Marketplace.API.Extensions;
using Marketplace.Application.Carts.Commands.AddCartItem;
using Marketplace.Application.Carts.Commands.CheckoutCart;
using Marketplace.Application.Carts.Commands.ClearCart;
using Marketplace.Application.Carts.Commands.RemoveCartItem;
using Marketplace.Application.Carts.Commands.UpdateCartItemQuantity;
using Marketplace.Application.Carts.Queries.GetMyCart;
using Marketplace.Domain.Orders.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("me/cart")]
[Authorize]
public sealed class CartController : ControllerBase
{
    private readonly ISender _sender;

    public CartController(ISender sender)
    {
        _sender = sender;
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
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new AddCartItemCommand(actorId, request.ProductId, request.Quantity), ct);
        return result.ToActionResult();
    }

    [HttpPatch("items/{itemId:long}")]
    public async Task<IActionResult> UpdateItemQuantity(long itemId, [FromBody] UpdateCartItemRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new UpdateCartItemQuantityCommand(actorId, itemId, request.Quantity), ct);
        return result.ToActionResult();
    }

    [HttpDelete("items/{itemId:long}")]
    public async Task<IActionResult> RemoveItem(long itemId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new RemoveCartItemCommand(actorId, itemId), ct);
        return result.ToActionResult();
    }

    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new ClearCartCommand(actorId), ct);
        return result.ToActionResult();
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutCartRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        if (!Enum.TryParse<CheckoutPaymentMethod>(request.PaymentMethod, true, out var paymentMethod))
            return BadRequest("Invalid paymentMethod");

        var command = new CheckoutCartCommand(
            actorId,
            paymentMethod,
            new CheckoutAddressDto(
                request.Address.FirstName,
                request.Address.LastName,
                request.Address.Phone,
                request.Address.Street,
                request.Address.City,
                request.Address.State,
                request.Address.PostalCode,
                request.Address.Country),
            request.Notes);

        var result = await _sender.Send(command, ct);
        return result.ToActionResult();
    }
}

public sealed record AddCartItemRequest(long ProductId, int Quantity);
public sealed record UpdateCartItemRequest(int Quantity);
public sealed record CheckoutCartRequest(string PaymentMethod, CheckoutAddressRequest Address, string? Notes);
public sealed record CheckoutAddressRequest(
    string FirstName,
    string LastName,
    string Phone,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country);

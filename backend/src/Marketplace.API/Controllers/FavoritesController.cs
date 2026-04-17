using Marketplace.API.Extensions;
using Marketplace.Application.Favorites.Commands.AddFavoriteProduct;
using Marketplace.Application.Favorites.Commands.RemoveFavoriteProduct;
using Marketplace.Application.Favorites.Queries.GetMyFavorites;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("me/favorites")]
[Authorize]
public sealed class FavoritesController : ControllerBase
{
    private readonly ISender _sender;

    public FavoritesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyFavorites(CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new GetMyFavoritesQuery(actorId), ct);
        return result.ToActionResult();
    }

    [HttpPost("{productId:long}")]
    public async Task<IActionResult> Add(long productId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new AddFavoriteProductCommand(actorId, productId), ct);
        return result.ToActionResult();
    }

    [HttpDelete("{productId:long}")]
    public async Task<IActionResult> Remove(long productId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new RemoveFavoriteProductCommand(actorId, productId), ct);
        return result.ToActionResult();
    }
}

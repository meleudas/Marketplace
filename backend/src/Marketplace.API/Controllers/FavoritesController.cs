using Marketplace.API.Extensions;
using Marketplace.Application.Favorites.Commands.AddFavoriteProduct;
using Marketplace.Application.Favorites.Commands.RemoveFavoriteProduct;
using Marketplace.Application.Favorites.Queries.GetMyFavorites;
using Marketplace.Application.Common.Observability;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("me/favorites")]
[Authorize]
public sealed class FavoritesController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<FavoritesController> _logger;

    public FavoritesController(ISender sender, ILogger<FavoritesController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyFavorites(CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.FavoriteLatencyMs, new KeyValuePair<string, object?>("operation", "favorites_list"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.FavoriteErrors.Add(1, [new KeyValuePair<string, object?>("operation", "favorites_list"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }
        var result = await _sender.Send(new GetMyFavoritesQuery(actorId), ct);
        RecordFavoritesResult("favorites_list", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpPost("{productId:long}")]
    public async Task<IActionResult> Add(long productId, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.FavoriteLatencyMs, new KeyValuePair<string, object?>("operation", "favorites_add"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.FavoriteErrors.Add(1, [new KeyValuePair<string, object?>("operation", "favorites_add"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }
        var result = await _sender.Send(new AddFavoriteProductCommand(actorId, productId), ct);
        RecordFavoritesResult("favorites_add", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpDelete("{productId:long}")]
    public async Task<IActionResult> Remove(long productId, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.FavoriteLatencyMs, new KeyValuePair<string, object?>("operation", "favorites_remove"));
        if (!User.TryGetUserId(out var actorId))
        {
            MarketplaceMetrics.FavoriteErrors.Add(1, [new KeyValuePair<string, object?>("operation", "favorites_remove"), new KeyValuePair<string, object?>("reason", "unauthorized")]);
            return Unauthorized();
        }
        var result = await _sender.Send(new RemoveFavoriteProductCommand(actorId, productId), ct);
        RecordFavoritesResult("favorites_remove", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    private void RecordFavoritesResult(string operation, bool success, string? error)
    {
        if (success)
        {
            MarketplaceMetrics.FavoriteOps.Add(1, [new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("status", "success")]);
            return;
        }

        MarketplaceMetrics.FavoriteErrors.Add(1, [new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("reason", "application_failure")]);
        _logger.LogWarning("Favorites operation {Operation} failed: {Error}", operation, error);
    }
}

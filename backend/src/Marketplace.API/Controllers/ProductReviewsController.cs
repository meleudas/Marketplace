using Marketplace.API.Extensions;
using Marketplace.Application.Reviews.Commands.CreateProductReview;
using Marketplace.Application.Reviews.Commands.DeleteOwnProductReview;
using Marketplace.Application.Reviews.Commands.UpdateOwnProductReview;
using Marketplace.Application.Reviews.Queries.GetProductReviews;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("products/{productId:long}/reviews")]
public sealed class ProductReviewsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductReviewsController(ISender sender) => _sender = sender;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(long productId, [FromQuery] int page = 1, [FromQuery] int size = 20, CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetProductReviewsQuery(productId, page, size), ct);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(long productId, [FromBody] UpsertProductReviewRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var userName = User.Identity?.Name ?? actorId.ToString();
        var result = await _sender.Send(new CreateProductReviewCommand(
            productId,
            actorId,
            userName,
            null,
            request.Rating,
            request.Title,
            request.Comment), ct);
        return result.ToActionResult();
    }

    [HttpPatch("{reviewId:long}")]
    [Authorize]
    public async Task<IActionResult> Update(long productId, long reviewId, [FromBody] UpsertProductReviewRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new UpdateOwnProductReviewCommand(reviewId, actorId, request.Rating, request.Title, request.Comment), ct);
        return result.ToActionResult();
    }

    [HttpDelete("{reviewId:long}")]
    [Authorize]
    public async Task<IActionResult> Delete(long productId, long reviewId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new DeleteOwnProductReviewCommand(reviewId, actorId), ct);
        return result.ToActionResult();
    }
}

public sealed record UpsertProductReviewRequest(byte Rating, string? Title, string Comment);

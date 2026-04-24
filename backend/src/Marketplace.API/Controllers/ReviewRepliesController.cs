using Marketplace.API.Extensions;
using Marketplace.Application.Reviews.Commands.UpsertCompanyReviewReply;
using Marketplace.Application.Reviews.Commands.UpsertProductReviewReply;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("reviews")]
[Authorize]
public sealed class ReviewRepliesController : ControllerBase
{
    private readonly ISender _sender;

    public ReviewRepliesController(ISender sender) => _sender = sender;

    [HttpPut("products/{reviewId:long}/reply")]
    public async Task<IActionResult> UpsertProductReply(long reviewId, [FromBody] UpsertReviewReplyRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new UpsertProductReviewReplyCommand(
            reviewId,
            actorId,
            User.IsInRole("Admin"),
            request.Body), ct);
        return result.ToActionResult();
    }

    [HttpPut("companies/{reviewId:long}/reply")]
    public async Task<IActionResult> UpsertCompanyReply(long reviewId, [FromBody] UpsertReviewReplyRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new UpsertCompanyReviewReplyCommand(
            reviewId,
            actorId,
            User.IsInRole("Admin"),
            request.Body), ct);
        return result.ToActionResult();
    }
}

public sealed record UpsertReviewReplyRequest(string Body);

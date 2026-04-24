using Marketplace.API.Extensions;
using Marketplace.Application.Reviews.Commands.CreateCompanyReview;
using Marketplace.Application.Reviews.Commands.DeleteOwnCompanyReview;
using Marketplace.Application.Reviews.Commands.UpdateOwnCompanyReview;
using Marketplace.Application.Reviews.Queries.GetCompanyReviews;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("companies/{companyId:guid}/reviews")]
public sealed class CompanyReviewsController : ControllerBase
{
    private readonly ISender _sender;

    public CompanyReviewsController(ISender sender) => _sender = sender;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(Guid companyId, [FromQuery] int page = 1, [FromQuery] int size = 20, CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetCompanyReviewsQuery(companyId, page, size), ct);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(Guid companyId, [FromBody] UpsertCompanyReviewRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var userName = User.Identity?.Name ?? actorId.ToString();
        var result = await _sender.Send(new CreateCompanyReviewCommand(
            companyId,
            actorId,
            userName,
            request.OverallRating,
            request.Comment), ct);
        return result.ToActionResult();
    }

    [HttpPatch("{reviewId:long}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid companyId, long reviewId, [FromBody] UpsertCompanyReviewRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new UpdateOwnCompanyReviewCommand(reviewId, actorId, request.OverallRating, request.Comment), ct);
        return result.ToActionResult();
    }

    [HttpDelete("{reviewId:long}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid companyId, long reviewId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new DeleteOwnCompanyReviewCommand(reviewId, actorId), ct);
        return result.ToActionResult();
    }
}

public sealed record UpsertCompanyReviewRequest(decimal OverallRating, string Comment);

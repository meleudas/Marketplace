using Marketplace.API.Extensions;
using Marketplace.Application.Reviews.Commands.ModerateCompanyReview;
using Marketplace.Application.Reviews.Commands.ModerateProductReview;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Reviews.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("admin/reviews")]
[Authorize(Roles = "Admin,Moderator")]
public sealed class AdminReviewsController : ControllerBase
{
    private readonly ISender _sender;

    public AdminReviewsController(ISender sender) => _sender = sender;

    [HttpPost("products/{reviewId:long}/moderate")]
    public async Task<IActionResult> ModerateProduct(long reviewId, [FromBody] ModerateProductReviewRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var canModerate = User.IsInRole("Admin") || User.IsInRole("Moderator");
        var result = await _sender.Send(new ModerateProductReviewCommand(reviewId, actorId, canModerate, request.Status), ct);
        return result.ToActionResult();
    }

    [HttpPost("companies/{reviewId:long}/moderate")]
    public async Task<IActionResult> ModerateCompany(long reviewId, [FromBody] ModerateCompanyReviewRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var canModerate = User.IsInRole("Admin") || User.IsInRole("Moderator");
        var result = await _sender.Send(new ModerateCompanyReviewCommand(reviewId, actorId, canModerate, request.Status), ct);
        return result.ToActionResult();
    }
}

public sealed record ModerateProductReviewRequest(ReviewModerationStatus Status);
public sealed record ModerateCompanyReviewRequest(CompanyReviewStatus Status);

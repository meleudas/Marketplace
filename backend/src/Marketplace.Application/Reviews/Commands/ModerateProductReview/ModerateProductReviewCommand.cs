using Marketplace.Application.Reviews.DTOs;
using Marketplace.Domain.Reviews.Enums;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reviews.Commands.ModerateProductReview;

public sealed record ModerateProductReviewCommand(
    long ReviewId,
    Guid ActorUserId,
    bool CanModerate,
    ReviewModerationStatus Status) : IRequest<Result<ReviewDto>>;

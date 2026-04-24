using Marketplace.Application.Reviews.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reviews.Commands.UpdateOwnProductReview;

public sealed record UpdateOwnProductReviewCommand(
    long ReviewId,
    Guid ActorUserId,
    byte Rating,
    string? Title,
    string Comment) : IRequest<Result<ReviewDto>>;

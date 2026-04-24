using Marketplace.Application.Reviews.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reviews.Commands.UpdateOwnCompanyReview;

public sealed record UpdateOwnCompanyReviewCommand(
    long ReviewId,
    Guid ActorUserId,
    decimal OverallRating,
    string Comment) : IRequest<Result<ReviewDto>>;

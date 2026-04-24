using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reviews.Commands.DeleteOwnProductReview;

public sealed record DeleteOwnProductReviewCommand(
    long ReviewId,
    Guid ActorUserId) : IRequest<Result>;

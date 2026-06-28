using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reviews.Commands.DeleteOwnCompanyReview;

public sealed record DeleteOwnCompanyReviewCommand(
    Guid CompanyId,
    long ReviewId,
    Guid ActorUserId) : IRequest<Result>;

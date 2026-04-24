using Marketplace.Application.Reviews.DTOs;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reviews.Commands.ModerateCompanyReview;

public sealed record ModerateCompanyReviewCommand(
    long ReviewId,
    Guid ActorUserId,
    bool CanModerate,
    CompanyReviewStatus Status) : IRequest<Result<ReviewDto>>;

using Marketplace.Application.Reviews.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reviews.Commands.CreateCompanyReview;

public sealed record CreateCompanyReviewCommand(
    Guid CompanyId,
    Guid ActorUserId,
    string UserName,
    decimal OverallRating,
    string Comment) : IRequest<Result<ReviewDto>>;

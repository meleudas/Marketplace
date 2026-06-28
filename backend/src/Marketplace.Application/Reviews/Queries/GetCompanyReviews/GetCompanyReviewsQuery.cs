using Marketplace.Application.Reviews.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reviews.Queries.GetCompanyReviews;

public sealed record GetCompanyReviewsQuery(
    Guid CompanyId,
    int Page = 1,
    int Size = 20) : IRequest<Result<ReviewListDto>>;

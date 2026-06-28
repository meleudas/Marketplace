using Marketplace.Application.Reviews.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reviews.Queries.GetProductReviews;

public sealed record GetProductReviewsQuery(
    long ProductId,
    int Page = 1,
    int Size = 20) : IRequest<Result<ReviewListDto>>;

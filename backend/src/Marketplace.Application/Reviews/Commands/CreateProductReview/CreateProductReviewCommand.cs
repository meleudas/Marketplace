using Marketplace.Application.Reviews.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reviews.Commands.CreateProductReview;

public sealed record CreateProductReviewCommand(
    long ProductId,
    Guid ActorUserId,
    string UserName,
    string? UserAvatar,
    byte Rating,
    string? Title,
    string Comment) : IRequest<Result<ReviewDto>>;

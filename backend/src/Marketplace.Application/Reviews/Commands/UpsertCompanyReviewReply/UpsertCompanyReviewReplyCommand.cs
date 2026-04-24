using Marketplace.Application.Reviews.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Reviews.Commands.UpsertCompanyReviewReply;

public sealed record UpsertCompanyReviewReplyCommand(
    long ReviewId,
    Guid ActorUserId,
    bool IsActorAdmin,
    string Body) : IRequest<Result<ReviewReplyDto>>;

using Marketplace.Application.Finance.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Finance.Queries.GetSellerEarningsSummary;

public sealed record GetSellerEarningsSummaryQuery(
    Guid CompanyId,
    Guid ActorUserId,
    bool IsActorAdmin,
    DateTime? FromUtc,
    DateTime? ToUtc) : IRequest<Result<SellerEarningsSummaryDto>>;

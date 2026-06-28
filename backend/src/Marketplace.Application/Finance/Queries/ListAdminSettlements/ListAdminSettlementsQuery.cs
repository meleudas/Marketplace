using Marketplace.Application.Finance.DTOs;
using Marketplace.Application.Finance.Queries.ListCompanySettlements;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Finance.Enums;
using Marketplace.Domain.Finance.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Finance.Queries.ListAdminSettlements;

public sealed record ListAdminSettlementsQuery(
    SettlementBatchStatus? Status,
    Guid? CompanyId) : IRequest<Result<IReadOnlyList<SettlementBatchDto>>>;

public sealed class ListAdminSettlementsQueryHandler
    : IRequestHandler<ListAdminSettlementsQuery, Result<IReadOnlyList<SettlementBatchDto>>>
{
    private readonly ISettlementBatchRepository _settlementBatchRepository;
    private readonly ISellerPayoutRepository _sellerPayoutRepository;

    public ListAdminSettlementsQueryHandler(
        ISettlementBatchRepository settlementBatchRepository,
        ISellerPayoutRepository sellerPayoutRepository)
    {
        _settlementBatchRepository = settlementBatchRepository;
        _sellerPayoutRepository = sellerPayoutRepository;
    }

    public async Task<Result<IReadOnlyList<SettlementBatchDto>>> Handle(ListAdminSettlementsQuery request, CancellationToken ct)
    {
        var companyId = request.CompanyId is null ? null : CompanyId.From(request.CompanyId.Value);
        var batches = await _settlementBatchRepository.ListAsync(request.Status, companyId, ct);
        var dtos = new List<SettlementBatchDto>();

        foreach (var batch in batches)
        {
            var payout = await _sellerPayoutRepository.GetBySettlementBatchIdAsync(batch.Id, ct);
            dtos.Add(ListCompanySettlementsQueryHandler.ToDto(batch, payout));
        }

        return Result<IReadOnlyList<SettlementBatchDto>>.Success(dtos);
    }
}

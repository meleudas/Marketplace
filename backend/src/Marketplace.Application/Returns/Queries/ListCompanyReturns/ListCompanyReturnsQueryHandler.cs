using Marketplace.Application.Returns.DTOs;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Returns.Enums;
using Marketplace.Domain.Returns.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Returns.Queries.ListCompanyReturns;

public sealed record ListCompanyReturnsQuery(Guid CompanyId, Guid ActorUserId, bool IsActorAdmin, string? Status) : IRequest<Result<IReadOnlyList<ReturnRequestSummaryDto>>>;

public sealed class ListCompanyReturnsQueryHandler : IRequestHandler<ListCompanyReturnsQuery, Result<IReadOnlyList<ReturnRequestSummaryDto>>>
{
    private readonly IReturnRequestRepository _returnRepository;
    private readonly ICompanyMemberRepository _companyMembers;

    public ListCompanyReturnsQueryHandler(
        IReturnRequestRepository returnRepository,
        ICompanyMemberRepository companyMembers)
    {
        _returnRepository = returnRepository;
        _companyMembers = companyMembers;
    }

    public async Task<Result<IReadOnlyList<ReturnRequestSummaryDto>>> Handle(ListCompanyReturnsQuery request, CancellationToken ct)
    {
        if (!request.IsActorAdmin)
        {
            var member = await _companyMembers.GetByCompanyAndUserAsync(
                CompanyId.From(request.CompanyId), request.ActorUserId, ct);
            if (member is null || member.IsDeleted)
                return Result<IReadOnlyList<ReturnRequestSummaryDto>>.Failure("Forbidden");
        }

        ReturnRequestStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<ReturnRequestStatus>(request.Status, true, out var parsed))
        {
            statusFilter = parsed;
        }

        var items = await _returnRepository.ListByCompanyAsync(CompanyId.From(request.CompanyId), statusFilter, ct);
        return Result<IReadOnlyList<ReturnRequestSummaryDto>>.Success(items.Select(ReturnMapper.ToSummary).ToList());
    }
}

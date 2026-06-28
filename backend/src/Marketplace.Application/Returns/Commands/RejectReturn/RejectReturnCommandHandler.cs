using Marketplace.Application.Returns.DTOs;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Returns.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Returns.Commands.RejectReturn;

public sealed record RejectReturnCommand(long ReturnId, Guid CompanyId, Guid ActorUserId, bool IsActorAdmin, string Reason) : IRequest<Result<ReturnRequestDetailDto>>;

public sealed class RejectReturnCommandHandler : IRequestHandler<RejectReturnCommand, Result<ReturnRequestDetailDto>>
{
    private readonly IReturnRequestRepository _returnRepository;
    private readonly IReturnLineItemRepository _returnLineItemRepository;
    private readonly ICompanyMemberRepository _companyMembers;

    public RejectReturnCommandHandler(
        IReturnRequestRepository returnRepository,
        IReturnLineItemRepository returnLineItemRepository,
        ICompanyMemberRepository companyMembers)
    {
        _returnRepository = returnRepository;
        _returnLineItemRepository = returnLineItemRepository;
        _companyMembers = companyMembers;
    }

    public async Task<Result<ReturnRequestDetailDto>> Handle(RejectReturnCommand request, CancellationToken ct)
    {
        var entity = await _returnRepository.GetByIdAsync(ReturnRequestId.From(request.ReturnId), ct);
        if (entity is null || entity.CompanyId.Value != request.CompanyId)
            return Result<ReturnRequestDetailDto>.Failure("Return request not found");

        if (!request.IsActorAdmin)
        {
            var member = await _companyMembers.GetByCompanyAndUserAsync(entity.CompanyId, request.ActorUserId, ct);
            if (member is null || member.IsDeleted)
                return Result<ReturnRequestDetailDto>.Failure("Forbidden");
        }

        try
        {
            entity.Reject(request.Reason);
            await _returnRepository.UpdateAsync(entity, ct);
            var lines = await _returnLineItemRepository.ListByReturnRequestIdAsync(entity.Id, ct);
            return Result<ReturnRequestDetailDto>.Success(ReturnMapper.ToDetail(entity, lines));
        }
        catch (Exception ex)
        {
            return Result<ReturnRequestDetailDto>.Failure(ex.Message);
        }
    }
}

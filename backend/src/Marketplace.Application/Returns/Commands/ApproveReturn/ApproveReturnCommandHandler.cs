using Marketplace.Application.Returns.DTOs;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Returns.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Returns.Commands.ApproveReturn;

public sealed record ApproveReturnCommand(long ReturnId, Guid CompanyId, Guid ActorUserId, bool IsActorAdmin) : IRequest<Result<ReturnRequestDetailDto>>;

public sealed class ApproveReturnCommandHandler : IRequestHandler<ApproveReturnCommand, Result<ReturnRequestDetailDto>>
{
    private readonly IReturnRequestRepository _returnRepository;
    private readonly IReturnLineItemRepository _returnLineItemRepository;
    private readonly ICompanyMemberRepository _companyMembers;

    public ApproveReturnCommandHandler(
        IReturnRequestRepository returnRepository,
        IReturnLineItemRepository returnLineItemRepository,
        ICompanyMemberRepository companyMembers)
    {
        _returnRepository = returnRepository;
        _returnLineItemRepository = returnLineItemRepository;
        _companyMembers = companyMembers;
    }

    public async Task<Result<ReturnRequestDetailDto>> Handle(ApproveReturnCommand request, CancellationToken ct)
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
            entity.Approve(request.ActorUserId);
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

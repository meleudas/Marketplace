using Marketplace.Application.Returns.DTOs;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Returns.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Returns.Queries.GetReturnById;

public sealed record GetReturnByIdQuery(long ReturnId, Guid ActorUserId, bool IsActorAdmin, bool IsCompanyScope, Guid? CompanyId) : IRequest<Result<ReturnRequestDetailDto>>;

public sealed class GetReturnByIdQueryHandler : IRequestHandler<GetReturnByIdQuery, Result<ReturnRequestDetailDto>>
{
    private readonly IReturnRequestRepository _returnRepository;
    private readonly IReturnLineItemRepository _returnLineItemRepository;

    public GetReturnByIdQueryHandler(
        IReturnRequestRepository returnRepository,
        IReturnLineItemRepository returnLineItemRepository)
    {
        _returnRepository = returnRepository;
        _returnLineItemRepository = returnLineItemRepository;
    }

    public async Task<Result<ReturnRequestDetailDto>> Handle(GetReturnByIdQuery request, CancellationToken ct)
    {
        var entity = await _returnRepository.GetByIdAsync(ReturnRequestId.From(request.ReturnId), ct);
        if (entity is null)
            return Result<ReturnRequestDetailDto>.Failure("Return request not found");

        if (request.IsCompanyScope)
        {
            if (!request.CompanyId.HasValue || entity.CompanyId.Value != request.CompanyId.Value)
                return Result<ReturnRequestDetailDto>.Failure("Return request not found");
        }
        else if (!request.IsActorAdmin && entity.CustomerId != request.ActorUserId)
        {
            return Result<ReturnRequestDetailDto>.Failure("Forbidden");
        }

        var lines = await _returnLineItemRepository.ListByReturnRequestIdAsync(entity.Id, ct);
        return Result<ReturnRequestDetailDto>.Success(ReturnMapper.ToDetail(entity, lines));
    }
}

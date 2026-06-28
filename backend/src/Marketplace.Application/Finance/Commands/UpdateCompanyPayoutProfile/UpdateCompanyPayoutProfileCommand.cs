using Marketplace.Application.Finance.Authorization;
using Marketplace.Application.Finance.DTOs;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Finance.Commands.UpdateCompanyPayoutProfile;

public sealed record UpdateCompanyPayoutProfileCommand(
    Guid CompanyId,
    Guid ActorUserId,
    bool IsActorAdmin,
    string? PayoutIban,
    string? PayoutRecipientName,
    string? PayoutProviderAccountId) : IRequest<Result<CompanyPayoutProfileDto>>;

public sealed class UpdateCompanyPayoutProfileCommandHandler
    : IRequestHandler<UpdateCompanyPayoutProfileCommand, Result<CompanyPayoutProfileDto>>
{
    private readonly IFinanceAccessService _access;
    private readonly ICompanyLegalProfileRepository _legalProfileRepository;

    public UpdateCompanyPayoutProfileCommandHandler(
        IFinanceAccessService access,
        ICompanyLegalProfileRepository legalProfileRepository)
    {
        _access = access;
        _legalProfileRepository = legalProfileRepository;
    }

    public async Task<Result<CompanyPayoutProfileDto>> Handle(UpdateCompanyPayoutProfileCommand request, CancellationToken ct)
    {
        if (!await _access.HasAccessAsync(
                request.CompanyId,
                request.ActorUserId,
                request.IsActorAdmin,
                FinancePermission.ManagePayoutProfile,
                ct))
            return Result<CompanyPayoutProfileDto>.Failure("Forbidden");

        var profile = await _legalProfileRepository.GetByCompanyIdAsync(CompanyId.From(request.CompanyId), ct);
        if (profile is null)
            return Result<CompanyPayoutProfileDto>.Failure("Company legal profile not found");

        profile.UpdatePayoutDetails(request.PayoutIban, request.PayoutRecipientName, request.PayoutProviderAccountId);
        await _legalProfileRepository.UpdateAsync(profile, ct);

        return Result<CompanyPayoutProfileDto>.Success(new CompanyPayoutProfileDto(
            request.CompanyId,
            profile.PayoutIban,
            profile.PayoutRecipientName,
            profile.PayoutProviderAccountId));
    }
}

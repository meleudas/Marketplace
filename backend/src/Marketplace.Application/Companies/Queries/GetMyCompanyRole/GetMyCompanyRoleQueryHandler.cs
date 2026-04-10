using Marketplace.Application.Companies.DTOs;
using Marketplace.Application.Companies.Mappings;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Queries.GetMyCompanyRole;

public sealed class GetMyCompanyRoleQueryHandler : IRequestHandler<GetMyCompanyRoleQuery, Result<CompanyMemberDto>>
{
    private readonly ICompanyMemberRepository _companyMemberRepository;

    public GetMyCompanyRoleQueryHandler(ICompanyMemberRepository companyMemberRepository)
    {
        _companyMemberRepository = companyMemberRepository;
    }

    public async Task<Result<CompanyMemberDto>> Handle(GetMyCompanyRoleQuery request, CancellationToken ct)
    {
        try
        {
            var membership = await _companyMemberRepository.GetByCompanyAndUserAsync(
                CompanyId.From(request.CompanyId),
                request.ActorUserId,
                ct);

            return membership is null
                ? Result<CompanyMemberDto>.Failure("Membership not found")
                : Result<CompanyMemberDto>.Success(CompanyMemberMapper.ToDto(membership));
        }
        catch (Exception ex)
        {
            return Result<CompanyMemberDto>.Failure($"Failed to get my company role: {ex.Message}");
        }
    }
}

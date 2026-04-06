using Marketplace.Application.Companies.DTOs;
using Marketplace.Application.Companies.Mappings;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Queries.GetApprovedCompanies;

public sealed class GetApprovedCompaniesQueryHandler : IRequestHandler<GetApprovedCompaniesQuery, Result<IReadOnlyList<CompanyDto>>>
{
    private readonly ICompanyRepository _companyRepository;

    public GetApprovedCompaniesQueryHandler(ICompanyRepository companyRepository)
    {
        _companyRepository = companyRepository;
    }

    public async Task<Result<IReadOnlyList<CompanyDto>>> Handle(GetApprovedCompaniesQuery request, CancellationToken ct)
    {
        try
        {
            var companies = await _companyRepository.GetApprovedAsync(ct);
            var dtos = companies.Select(CompanyMapper.ToDto).ToList();
            return Result<IReadOnlyList<CompanyDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<CompanyDto>>.Failure($"Failed to get approved companies: {ex.Message}");
        }
    }
}

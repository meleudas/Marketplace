using Marketplace.Application.Companies.DTOs;
using Marketplace.Application.Companies.Mappings;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Queries.GetAllCompanies;

public sealed class GetAllCompaniesQueryHandler : IRequestHandler<GetAllCompaniesQuery, Result<IReadOnlyList<CompanyDto>>>
{
    private readonly ICompanyRepository _companyRepository;

    public GetAllCompaniesQueryHandler(ICompanyRepository companyRepository)
    {
        _companyRepository = companyRepository;
    }

    public async Task<Result<IReadOnlyList<CompanyDto>>> Handle(GetAllCompaniesQuery request, CancellationToken ct)
    {
        try
        {
            var companies = await _companyRepository.GetAllAsync(ct);
            var dtos = companies.Select(CompanyMapper.ToDto).ToList();
            return Result<IReadOnlyList<CompanyDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<CompanyDto>>.Failure($"Failed to get companies: {ex.Message}");
        }
    }
}

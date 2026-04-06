using Marketplace.Application.Companies.DTOs;
using Marketplace.Application.Companies.Mappings;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Commands.UpdateCompany;

public sealed class UpdateCompanyCommandHandler : IRequestHandler<UpdateCompanyCommand, Result<CompanyDto>>
{
    private readonly ICompanyRepository _companyRepository;

    public UpdateCompanyCommandHandler(ICompanyRepository companyRepository)
    {
        _companyRepository = companyRepository;
    }

    public async Task<Result<CompanyDto>> Handle(UpdateCompanyCommand request, CancellationToken ct)
    {
        try
        {
            var id = CompanyId.From(request.CompanyId);
            var company = await _companyRepository.GetByIdAsync(id, ct);
            if (company == null)
                return Result<CompanyDto>.Failure("Company not found");

            company.UpdateProfile(
                request.Name,
                request.Slug,
                request.Description,
                request.ImageUrl,
                request.ContactEmail,
                request.ContactPhone,
                CompanyMapper.ToAddress(request.Address),
                new JsonBlob(request.MetaRaw));

            await _companyRepository.UpdateAsync(company, ct);
            return Result<CompanyDto>.Success(CompanyMapper.ToDto(company));
        }
        catch (Exception ex)
        {
            return Result<CompanyDto>.Failure($"Failed to update company: {ex.Message}");
        }
    }
}

using Marketplace.Application.Companies.DTOs;
using Marketplace.Application.Companies.Mappings;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Commands.CreateCompany;

public sealed class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, Result<CompanyDto>>
{
    private readonly ICompanyRepository _companyRepository;

    public CreateCompanyCommandHandler(ICompanyRepository companyRepository)
    {
        _companyRepository = companyRepository;
    }

    public async Task<Result<CompanyDto>> Handle(CreateCompanyCommand request, CancellationToken ct)
    {
        try
        {
            var id = CompanyId.From(Guid.NewGuid());

            var company = Company.Create(
                id,
                request.Name,
                request.Slug,
                request.Description,
                request.ImageUrl,
                request.ContactEmail,
                request.ContactPhone,
                CompanyMapper.ToAddress(request.Address),
                new JsonBlob(request.MetaRaw));

            await _companyRepository.AddAsync(company, ct);
            return Result<CompanyDto>.Success(CompanyMapper.ToDto(company));
        }
        catch (Exception ex)
        {
            return Result<CompanyDto>.Failure($"Failed to create company: {ex.Message}");
        }
    }
}

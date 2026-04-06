using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Commands.DeleteCompany;

public sealed class DeleteCompanyCommandHandler : IRequestHandler<DeleteCompanyCommand, Result>
{
    private readonly ICompanyRepository _companyRepository;

    public DeleteCompanyCommandHandler(ICompanyRepository companyRepository)
    {
        _companyRepository = companyRepository;
    }

    public async Task<Result> Handle(DeleteCompanyCommand request, CancellationToken ct)
    {
        try
        {
            var company = await _companyRepository.GetByIdAsync(CompanyId.From(request.CompanyId), ct);
            if (company == null)
                return Result.Failure("Company not found");

            company.SoftDelete();
            await _companyRepository.UpdateAsync(company, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete company: {ex.Message}");
        }
    }
}

using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Commands.RevokeCompanyApproval;

public sealed class RevokeCompanyApprovalCommandHandler : IRequestHandler<RevokeCompanyApprovalCommand, Result>
{
    private readonly ICompanyRepository _companyRepository;

    public RevokeCompanyApprovalCommandHandler(ICompanyRepository companyRepository)
    {
        _companyRepository = companyRepository;
    }

    public async Task<Result> Handle(RevokeCompanyApprovalCommand request, CancellationToken ct)
    {
        try
        {
            var company = await _companyRepository.GetByIdAsync(CompanyId.From(request.CompanyId), ct);
            if (company == null)
                return Result.Failure("Company not found");

            company.RevokeApproval();
            await _companyRepository.UpdateAsync(company, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to revoke company approval: {ex.Message}");
        }
    }
}

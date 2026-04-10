using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Companies.Repositories;

namespace Marketplace.Application.Products.Authorization;

public interface IProductAccessService
{
    Task<bool> HasAccessAsync(Guid companyId, Guid actorUserId, bool isActorAdmin, ProductPermission permission, CancellationToken ct = default);
}

public sealed class ProductAccessService : IProductAccessService
{
    private readonly ICompanyMemberRepository _companyMemberRepository;

    public ProductAccessService(ICompanyMemberRepository companyMemberRepository)
    {
        _companyMemberRepository = companyMemberRepository;
    }

    public async Task<bool> HasAccessAsync(Guid companyId, Guid actorUserId, bool isActorAdmin, ProductPermission permission, CancellationToken ct = default)
    {
        if (isActorAdmin)
            return true;

        var member = await _companyMemberRepository.GetByCompanyAndUserAsync(CompanyId.From(companyId), actorUserId, ct);
        if (member is null)
            return false;

        return permission switch
        {
            ProductPermission.ReadInternal => true,
            ProductPermission.WriteProduct => member.Role is CompanyMembershipRole.Owner or CompanyMembershipRole.Manager or CompanyMembershipRole.Seller,
            _ => false
        };
    }
}

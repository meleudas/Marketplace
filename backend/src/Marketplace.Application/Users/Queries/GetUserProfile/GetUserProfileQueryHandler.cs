using Marketplace.Application.Auth.DTOs;
using Marketplace.Application.Auth.Mappings;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Users.Cache;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.Repositories;
using Marketplace.Domain.Users.ValueObjects;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Users.Queries.GetUserProfile;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICompanyMemberRepository _companyMemberRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IAppCachePort _cache;
    private readonly CacheTtlOptions _ttl;

    public GetUserProfileQueryHandler(
        IUserRepository userRepository,
        ICompanyMemberRepository companyMemberRepository,
        ICompanyRepository companyRepository,
        IAppCachePort cache,
        IOptions<CacheTtlOptions> ttl)
    {
        _userRepository = userRepository;
        _companyMemberRepository = companyMemberRepository;
        _companyRepository = companyRepository;
        _cache = cache;
        _ttl = ttl.Value;
    }

    public async Task<Result<UserDto>> Handle(GetUserProfileQuery request, CancellationToken ct)
    {
        try
        {
            var cacheKey = UserCacheKeys.Profile(request.IdentityUserId);
            var cached = await _cache.GetAsync<UserDto>(cacheKey, ct);
            if (cached is not null)
                return Result<UserDto>.Success(cached);

            var identityId = IdentityUserId.From(request.IdentityUserId);
            var user = await _userRepository.GetByIdentityIdAsync(identityId, ct);

            if (user == null)
                return Result<UserDto>.Failure("User not found");

            var members = await _companyMemberRepository.ListByUserAsync(request.IdentityUserId, ct);
            var companyRows = new List<UserCompanyMembershipDto>();
            foreach (var m in members)
            {
                var company = await _companyRepository.GetByIdAsync(m.CompanyId, ct);
                if (company is null || company.IsDeleted)
                    continue;

                companyRows.Add(new UserCompanyMembershipDto(
                    company.Id.Value,
                    company.Name,
                    company.Slug,
                    m.Role.ToString().ToLowerInvariant(),
                    m.IsOwner));
            }

            var dto = AuthMapper.ToUserDto(user, companyRows);
            await _cache.SetAsync(cacheKey, dto, _ttl.UsersProfile, ct);
            return Result<UserDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<UserDto>.Failure($"Failed to get user profile: {ex.Message}");
        }
    }
}

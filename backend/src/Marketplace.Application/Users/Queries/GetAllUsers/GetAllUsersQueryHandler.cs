using Marketplace.Application.Auth.DTOs;
using Marketplace.Application.Auth.Mappings;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Users.Cache;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.Repositories;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Users.Queries.GetAllUsers;

public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, Result<IReadOnlyList<UserDto>>>
{
    private readonly IUserRepository _userRepository;
    private readonly IAppCachePort _cache;
    private readonly CacheTtlOptions _ttl;

    public GetAllUsersQueryHandler(IUserRepository userRepository, IAppCachePort cache, IOptions<CacheTtlOptions> ttl)
    {
        _userRepository = userRepository;
        _cache = cache;
        _ttl = ttl.Value;
    }

    public async Task<Result<IReadOnlyList<UserDto>>> Handle(GetAllUsersQuery request, CancellationToken ct)
    {
        try
        {
            var cached = await _cache.GetAsync<List<UserDto>>(UserCacheKeys.All, ct);
            if (cached is not null)
                return Result<IReadOnlyList<UserDto>>.Success(cached);

            var users = await _userRepository.GetAllAsync(ct);
            var dtos = users.Select(u => AuthMapper.ToUserDto(u)).ToList();
            await _cache.SetAsync(UserCacheKeys.All, dtos, _ttl.UsersList, ct);
            return Result<IReadOnlyList<UserDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<UserDto>>.Failure($"Failed to get users: {ex.Message}");
        }
    }
}

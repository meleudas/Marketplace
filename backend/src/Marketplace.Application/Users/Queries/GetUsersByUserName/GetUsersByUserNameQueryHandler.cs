using Marketplace.Application.Auth.DTOs;
using Marketplace.Application.Auth.Mappings;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Users.Cache;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.Repositories;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Users.Queries.GetUsersByUserName;

public class GetUsersByUserNameQueryHandler : IRequestHandler<GetUsersByUserNameQuery, Result<IReadOnlyList<UserDto>>>
{
    private readonly IUserRepository _userRepository;
    private readonly IAppCachePort _cache;
    private readonly CacheTtlOptions _ttl;

    public GetUsersByUserNameQueryHandler(IUserRepository userRepository, IAppCachePort cache, IOptions<CacheTtlOptions> ttl)
    {
        _userRepository = userRepository;
        _cache = cache;
        _ttl = ttl.Value;
    }

    public async Task<Result<IReadOnlyList<UserDto>>> Handle(GetUsersByUserNameQuery request, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.UserName))
                return Result<IReadOnlyList<UserDto>>.Failure("UserName is required");

            var cacheKey = UserCacheKeys.SearchByUserName(request.UserName);
            var cached = await _cache.GetAsync<List<UserDto>>(cacheKey, ct);
            if (cached is not null)
                return Result<IReadOnlyList<UserDto>>.Success(cached);

            var users = await _userRepository.SearchByUserNameAsync(request.UserName, ct);
            var dtos = users.Select(u => AuthMapper.ToUserDto(u)).ToList();
            await _cache.SetAsync(cacheKey, dtos, _ttl.UsersSearch, ct);
            return Result<IReadOnlyList<UserDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<UserDto>>.Failure($"Failed to search users: {ex.Message}");
        }
    }
}

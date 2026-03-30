using Marketplace.Application.Auth.DTOs;
using Marketplace.Application.Auth.Mappings;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.Repositories;
using MediatR;

namespace Marketplace.Application.Users.Queries.GetAllUsers;

public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, Result<IReadOnlyList<UserDto>>>
{
    private readonly IUserRepository _userRepository;

    public GetAllUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<IReadOnlyList<UserDto>>> Handle(GetAllUsersQuery request, CancellationToken ct)
    {
        try
        {
            var users = await _userRepository.GetAllAsync(ct);
            var dtos = users.Select(AuthMapper.ToUserDto).ToList();
            return Result<IReadOnlyList<UserDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<UserDto>>.Failure($"Failed to get users: {ex.Message}");
        }
    }
}

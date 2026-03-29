using Marketplace.Application.Auth.DTOs;
using Marketplace.Application.Auth.Mappings;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.Repositories;
using MediatR;

namespace Marketplace.Application.Users.Queries.GetUsersByUserName;

public class GetUsersByUserNameQueryHandler : IRequestHandler<GetUsersByUserNameQuery, Result<IReadOnlyList<UserDto>>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersByUserNameQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<IReadOnlyList<UserDto>>> Handle(GetUsersByUserNameQuery request, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.UserName))
                return Result<IReadOnlyList<UserDto>>.Failure("UserName is required");

            var users = await _userRepository.SearchByUserNameAsync(request.UserName, ct);
            var dtos = users.Select(AuthMapper.ToUserDto).ToList();
            return Result<IReadOnlyList<UserDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<UserDto>>.Failure($"Failed to search users: {ex.Message}");
        }
    }
}

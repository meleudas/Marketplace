using Marketplace.Application.Auth.DTOs;
using Marketplace.Application.Auth.Mappings;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.Repositories;
using Marketplace.Domain.Users.ValueObjects;
using MediatR;


namespace Marketplace.Application.Users.Queries.GetUserProfile
{
    public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, Result<UserDto>>
    {
        private readonly IUserRepository _userRepository;

        public GetUserProfileQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<UserDto>> Handle(GetUserProfileQuery request, CancellationToken ct)
        {
            try
            {
                var identityId = IdentityUserId.From(request.IdentityUserId);
                var user = await _userRepository.GetByIdentityIdAsync(identityId, ct);

                if (user == null)
                    return Result<UserDto>.Failure("User not found");

                var dto = AuthMapper.ToUserDto(user);
                return Result<UserDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<UserDto>.Failure($"Failed to get user profile: {ex.Message}");
            }
        }
    }
}

using Marketplace.Application.Auth.Ports;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Users.Cache;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.Repositories;
using Marketplace.Domain.Users.ValueObjects;
using MediatR;
using System;
using System.Linq;

namespace Marketplace.Application.Users.Commands.UpdateMyProfile;

public sealed class UpdateMyProfileCommandHandler : IRequestHandler<UpdateMyProfileCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthenticationPort _authenticationPort;
    private readonly IAppCachePort _cache;

    public UpdateMyProfileCommandHandler(
        IUserRepository userRepository,
        IAuthenticationPort authenticationPort,
        IAppCachePort cache)
    {
        _userRepository = userRepository;
        _authenticationPort = authenticationPort;
        _cache = cache;
    }

    public async Task<Result> Handle(UpdateMyProfileCommand request, CancellationToken ct)
    {
        try
        {
            var identityId = IdentityUserId.From(request.IdentityUserId);
            var user = await _userRepository.GetByIdentityIdAsync(identityId, ct);
            if (user is null || user.IsDeleted)
                return Result.Failure("User not found");

            var nameParts = request.UserName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var firstName = nameParts.Length > 0 ? nameParts[0] : request.UserName.Trim();
            var lastName = nameParts.Length > 1 ? string.Join(' ', nameParts.Skip(1)) : "-";
            user.UpdateProfile(firstName, lastName, user.Birthday, user.Avatar);
            await _userRepository.UpdateAsync(user, ct);

            var identityUpdate = await _authenticationPort.UpdateProfileAsync(
                identityId,
                UserName.Create(request.UserName),
                request.PhoneNumber,
                ct);

            if (identityUpdate.IsFailure)
                return identityUpdate;

            await _cache.RemoveAsync(UserCacheKeys.Profile(request.IdentityUserId), ct);
            await _cache.RemoveAsync(UserCacheKeys.SearchByUserName(request.UserName), ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Update profile failed: {ex.Message}");
        }
    }
}

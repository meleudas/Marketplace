using Marketplace.Application.Auth.Ports;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Users.Cache;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.ValueObjects;
using MediatR;

namespace Marketplace.Application.Users.Commands.DeleteUser;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result>
{
    private readonly IAuthenticationPort _authenticationPort;
    private readonly IAppCachePort _cache;

    public DeleteUserCommandHandler(IAuthenticationPort authenticationPort, IAppCachePort cache)
    {
        _authenticationPort = authenticationPort;
        _cache = cache;
    }

    public async Task<Result> Handle(DeleteUserCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _authenticationPort.DeleteAccountAsync(IdentityUserId.From(request.IdentityUserId), ct);
            if (result.IsSuccess)
            {
                await _cache.RemoveAsync(UserCacheKeys.All, ct);
                await _cache.RemoveAsync(UserCacheKeys.Profile(request.IdentityUserId), ct);
            }

            return result;
        }
        catch (Exception ex)
        {
            return Result.Failure($"Delete account failed: {ex.Message}");
        }
    }
}

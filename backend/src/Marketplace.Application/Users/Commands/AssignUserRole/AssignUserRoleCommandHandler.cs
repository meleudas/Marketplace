using Marketplace.Application.Auth.Ports;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Users.Cache;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.ValueObjects;
using MediatR;

namespace Marketplace.Application.Users.Commands.AssignUserRole;

public sealed class AssignUserRoleCommandHandler : IRequestHandler<AssignUserRoleCommand, Result>
{
    private readonly IAuthenticationPort _authenticationPort;
    private readonly IAppCachePort _cache;

    public AssignUserRoleCommandHandler(IAuthenticationPort authenticationPort, IAppCachePort cache)
    {
        _authenticationPort = authenticationPort;
        _cache = cache;
    }

    public async Task<Result> Handle(AssignUserRoleCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _authenticationPort.AssignUserRoleAsync(IdentityUserId.From(request.IdentityUserId), request.Role, ct);
            if (result.IsSuccess)
            {
                await _cache.RemoveAsync(UserCacheKeys.All, ct);
                await _cache.RemoveAsync(UserCacheKeys.Profile(request.IdentityUserId), ct);
            }

            return result;
        }
        catch (Exception ex)
        {
            return Result.Failure($"Assign role failed: {ex.Message}");
        }
    }
}

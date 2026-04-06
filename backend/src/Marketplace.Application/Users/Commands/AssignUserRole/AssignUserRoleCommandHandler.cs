using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.ValueObjects;
using MediatR;

namespace Marketplace.Application.Users.Commands.AssignUserRole;

public sealed class AssignUserRoleCommandHandler : IRequestHandler<AssignUserRoleCommand, Result>
{
    private readonly IAuthenticationPort _authenticationPort;

    public AssignUserRoleCommandHandler(IAuthenticationPort authenticationPort)
    {
        _authenticationPort = authenticationPort;
    }

    public async Task<Result> Handle(AssignUserRoleCommand request, CancellationToken ct)
    {
        try
        {
            return await _authenticationPort.AssignUserRoleAsync(IdentityUserId.From(request.IdentityUserId), request.Role, ct);
        }
        catch (Exception ex)
        {
            return Result.Failure($"Assign role failed: {ex.Message}");
        }
    }
}

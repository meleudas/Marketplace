using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.ValueObjects;
using MediatR;

namespace Marketplace.Application.Users.Commands.DeleteUser;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result>
{
    private readonly IAuthenticationPort _authenticationPort;

    public DeleteUserCommandHandler(IAuthenticationPort authenticationPort) =>
        _authenticationPort = authenticationPort;

    public async Task<Result> Handle(DeleteUserCommand request, CancellationToken ct)
    {
        try
        {
            return await _authenticationPort.DeleteAccountAsync(IdentityUserId.From(request.IdentityUserId), ct);
        }
        catch (Exception ex)
        {
            return Result.Failure($"Delete account failed: {ex.Message}");
        }
    }
}

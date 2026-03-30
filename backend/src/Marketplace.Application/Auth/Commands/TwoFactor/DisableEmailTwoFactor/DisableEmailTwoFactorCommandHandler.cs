using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.ValueObjects;
using MediatR;

namespace Marketplace.Application.Auth.Commands.TwoFactor.DisableEmailTwoFactor;

public sealed class DisableEmailTwoFactorCommandHandler : IRequestHandler<DisableEmailTwoFactorCommand, Result>
{
    private readonly IAuthenticationPort _authenticationPort;

    public DisableEmailTwoFactorCommandHandler(IAuthenticationPort authenticationPort)
    {
        _authenticationPort = authenticationPort;
    }

    public Task<Result> Handle(DisableEmailTwoFactorCommand request, CancellationToken ct)
    {
        return _authenticationPort.DisableEmailTwoFactorAsync(IdentityUserId.From(request.IdentityUserId), ct);
    }
}


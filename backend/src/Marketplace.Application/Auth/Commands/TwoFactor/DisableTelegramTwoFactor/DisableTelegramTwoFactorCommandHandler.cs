using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.ValueObjects;
using MediatR;

namespace Marketplace.Application.Auth.Commands.TwoFactor.DisableTelegramTwoFactor;

public sealed class DisableTelegramTwoFactorCommandHandler : IRequestHandler<DisableTelegramTwoFactorCommand, Result>
{
    private readonly IAuthenticationPort _authenticationPort;

    public DisableTelegramTwoFactorCommandHandler(IAuthenticationPort authenticationPort)
    {
        _authenticationPort = authenticationPort;
    }

    public Task<Result> Handle(DisableTelegramTwoFactorCommand request, CancellationToken ct)
    {
        return _authenticationPort.DisableTelegramTwoFactorAsync(IdentityUserId.From(request.IdentityUserId), ct);
    }
}

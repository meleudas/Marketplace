using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.ValueObjects;
using MediatR;

namespace Marketplace.Application.Auth.Commands.TwoFactor.EnableTelegramTwoFactor;

public sealed class EnableTelegramTwoFactorCommandHandler : IRequestHandler<EnableTelegramTwoFactorCommand, Result>
{
    private readonly IAuthenticationPort _authenticationPort;

    public EnableTelegramTwoFactorCommandHandler(IAuthenticationPort authenticationPort)
    {
        _authenticationPort = authenticationPort;
    }

    public Task<Result> Handle(EnableTelegramTwoFactorCommand request, CancellationToken ct)
    {
        return _authenticationPort.EnableTelegramTwoFactorAsync(IdentityUserId.From(request.IdentityUserId), request.Code, ct);
    }
}

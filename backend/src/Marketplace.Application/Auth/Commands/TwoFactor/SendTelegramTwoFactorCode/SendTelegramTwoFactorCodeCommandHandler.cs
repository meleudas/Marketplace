using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.ValueObjects;
using MediatR;

namespace Marketplace.Application.Auth.Commands.TwoFactor.SendTelegramTwoFactorCode;

public sealed class SendTelegramTwoFactorCodeCommandHandler : IRequestHandler<SendTelegramTwoFactorCodeCommand, Result>
{
    private readonly IAuthenticationPort _authenticationPort;

    public SendTelegramTwoFactorCodeCommandHandler(IAuthenticationPort authenticationPort)
    {
        _authenticationPort = authenticationPort;
    }

    public Task<Result> Handle(SendTelegramTwoFactorCodeCommand request, CancellationToken ct)
    {
        return _authenticationPort.SendTelegramTwoFactorCodeAsync(IdentityUserId.From(request.IdentityUserId), ct);
    }
}

using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.ValueObjects;
using MediatR;

namespace Marketplace.Application.Auth.Commands.TwoFactor.SendEmailTwoFactorCode;

public sealed class SendEmailTwoFactorCodeCommandHandler : IRequestHandler<SendEmailTwoFactorCodeCommand, Result>
{
    private readonly IAuthenticationPort _authenticationPort;

    public SendEmailTwoFactorCodeCommandHandler(IAuthenticationPort authenticationPort)
    {
        _authenticationPort = authenticationPort;
    }

    public Task<Result> Handle(SendEmailTwoFactorCodeCommand request, CancellationToken ct)
    {
        return _authenticationPort.SendEmailTwoFactorCodeAsync(IdentityUserId.From(request.IdentityUserId), ct);
    }
}


using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.ValueObjects;
using MediatR;

namespace Marketplace.Application.Auth.Commands.TwoFactor.EnableEmailTwoFactor;

public sealed class EnableEmailTwoFactorCommandHandler : IRequestHandler<EnableEmailTwoFactorCommand, Result>
{
    private readonly IAuthenticationPort _authenticationPort;

    public EnableEmailTwoFactorCommandHandler(IAuthenticationPort authenticationPort)
    {
        _authenticationPort = authenticationPort;
    }

    public Task<Result> Handle(EnableEmailTwoFactorCommand request, CancellationToken ct)
    {
        return _authenticationPort.EnableEmailTwoFactorAsync(IdentityUserId.From(request.IdentityUserId), request.Code, ct);
    }
}


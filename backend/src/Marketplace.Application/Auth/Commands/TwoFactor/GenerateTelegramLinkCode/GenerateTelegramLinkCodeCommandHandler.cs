using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.ValueObjects;
using MediatR;

namespace Marketplace.Application.Auth.Commands.TwoFactor.GenerateTelegramLinkCode;

public sealed class GenerateTelegramLinkCodeCommandHandler : IRequestHandler<GenerateTelegramLinkCodeCommand, Result<string>>
{
    private readonly IAuthenticationPort _authenticationPort;

    public GenerateTelegramLinkCodeCommandHandler(IAuthenticationPort authenticationPort)
    {
        _authenticationPort = authenticationPort;
    }

    public Task<Result<string>> Handle(GenerateTelegramLinkCodeCommand request, CancellationToken ct)
    {
        return _authenticationPort.GenerateTelegramLinkCodeAsync(IdentityUserId.From(request.IdentityUserId), ct);
    }
}

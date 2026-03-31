using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Auth.Commands.TwoFactor.LinkTelegramAccount;

public sealed class LinkTelegramAccountCommandHandler : IRequestHandler<LinkTelegramAccountCommand, Result>
{
    private readonly IAuthenticationPort _authenticationPort;

    public LinkTelegramAccountCommandHandler(IAuthenticationPort authenticationPort)
    {
        _authenticationPort = authenticationPort;
    }

    public Task<Result> Handle(LinkTelegramAccountCommand request, CancellationToken ct)
    {
        return _authenticationPort.LinkTelegramAccountAsync(request.LinkCode, request.ChatId, ct);
    }
}

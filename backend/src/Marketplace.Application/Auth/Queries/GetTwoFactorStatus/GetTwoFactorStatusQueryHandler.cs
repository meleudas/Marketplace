using Marketplace.Application.Auth.DTOs;
using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.ValueObjects;
using MediatR;

namespace Marketplace.Application.Auth.Queries.GetTwoFactorStatus;

public sealed class GetTwoFactorStatusQueryHandler : IRequestHandler<GetTwoFactorStatusQuery, Result<TwoFactorStatusDto>>
{
    private readonly IAuthenticationPort _authenticationPort;

    public GetTwoFactorStatusQueryHandler(IAuthenticationPort authenticationPort) =>
        _authenticationPort = authenticationPort;

    public Task<Result<TwoFactorStatusDto>> Handle(GetTwoFactorStatusQuery request, CancellationToken ct) =>
        _authenticationPort.GetTwoFactorStatusAsync(IdentityUserId.From(request.IdentityUserId), ct);
}

using Marketplace.Application.Auth.Mappings;
using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Users.Commands.VerifyEmail;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result>
{
    private readonly IAuthenticationPort _authenticationPort;

    public VerifyEmailCommandHandler(IAuthenticationPort authenticationPort)
    {
        _authenticationPort = authenticationPort;
    }

    public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken ct)
    {
        try
        {
            var email = AuthMapper.ToEmail(request.Email);
            return await _authenticationPort.ConfirmEmailAsync(email, request.Token, ct);
        }
        catch (Exception ex)
        {
            return Result.Failure($"Email verification failed: {ex.Message}");
        }
    }
}

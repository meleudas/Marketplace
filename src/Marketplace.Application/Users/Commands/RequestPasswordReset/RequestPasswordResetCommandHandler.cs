using Marketplace.Application.Auth.Mappings;
using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Users.Commands.RequestPasswordReset
{
    public class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, Result>
    {
        private readonly IAuthenticationPort _authenticationPort;
        private readonly IEmailPort _emailPort;

        public RequestPasswordResetCommandHandler(
            IAuthenticationPort authenticationPort,
            IEmailPort emailPort)
        {
            _authenticationPort = authenticationPort;
            _emailPort = emailPort;
        }

        public async Task<Result> Handle(RequestPasswordResetCommand request, CancellationToken ct)
        {
            try
            {
                var email = AuthMapper.ToEmail(request.Email);
                var tokenResult = await _authenticationPort.GeneratePasswordResetTokenAsync(email, ct);

                if (tokenResult.IsFailure)
                    return Result.Success();

                await _emailPort.SendPasswordResetEmailAsync(email.Value, tokenResult.Value!, ct);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Password reset request failed: {ex.Message}");
            }
        }
    }
}

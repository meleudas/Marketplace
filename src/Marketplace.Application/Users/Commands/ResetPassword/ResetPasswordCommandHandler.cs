using Marketplace.Application.Auth.Mappings;
using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Users.Commands.ResetPassword
{
    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
    {
        private readonly IAuthenticationPort _authenticationPort;

        public ResetPasswordCommandHandler(IAuthenticationPort authenticationPort)
        {
            _authenticationPort = authenticationPort;
        }

        public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken ct)
        {
            try
            {
                var email = AuthMapper.ToEmail(request.Email);
                return await _authenticationPort.ResetPasswordAsync(email, request.Token, request.NewPassword, ct);
            }
            catch (Exception ex)
            {
                return Result.Failure($"Password reset failed: {ex.Message}");
            }
        }
    }
}

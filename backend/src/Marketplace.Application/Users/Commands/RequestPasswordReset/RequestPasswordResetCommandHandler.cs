using Marketplace.Application.Auth.Mappings;
using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Users.Commands.RequestPasswordReset
{
    public class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, Result>
    {
        private readonly IAuthenticationPort _authenticationPort;
        private readonly INotificationDispatcher _notificationDispatcher;

        public RequestPasswordResetCommandHandler(
            IAuthenticationPort authenticationPort,
            INotificationDispatcher notificationDispatcher)
        {
            _authenticationPort = authenticationPort;
            _notificationDispatcher = notificationDispatcher;
        }

        public async Task<Result> Handle(RequestPasswordResetCommand request, CancellationToken ct)
        {
            try
            {
                var email = AuthMapper.ToEmail(request.Email);
                var tokenResult = await _authenticationPort.GeneratePasswordResetTokenAsync(email, ct);

                if (tokenResult.IsFailure)
                    return Result.Success();

                await _notificationDispatcher.EnqueuePasswordResetEmailAsync(email.Value, tokenResult.Value!, ct);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Password reset request failed: {ex.Message}");
            }
        }
    }
}

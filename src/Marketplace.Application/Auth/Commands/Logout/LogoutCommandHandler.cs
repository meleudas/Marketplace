using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.ValueObjects;
using MediatR;

namespace Marketplace.Application.Auth.Commands.Logout
{
    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
    {
        private readonly IAuthenticationPort _authenticationPort;

        public LogoutCommandHandler(IAuthenticationPort authenticationPort)
        {
            _authenticationPort = authenticationPort;
        }

        public async Task<Result> Handle(LogoutCommand request, CancellationToken ct)
        {
            try
            {
                var userId = IdentityUserId.From(request.UserId);
                var result = await _authenticationPort.LogoutAsync(userId, ct);
                return result;
            }
            catch (Exception ex)
            {
                return Result.Failure($"Logout failed: {ex.Message}");
            }
        }
    }
}

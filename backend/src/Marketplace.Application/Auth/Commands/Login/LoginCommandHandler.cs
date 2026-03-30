using Marketplace.Application.Auth.DTOs;
using Marketplace.Application.Auth.Mappings;
using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Shared.Kernel;
using MediatR;


namespace Marketplace.Application.Auth.Commands.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthTokensDto>>
    {
        private readonly IAuthenticationPort _authenticationPort;

        public LoginCommandHandler(IAuthenticationPort authenticationPort)
        {
            _authenticationPort = authenticationPort;
        }

        public async Task<Result<AuthTokensDto>> Handle(LoginCommand request, CancellationToken ct)
        {
            try
            {
                var email = AuthMapper.ToEmail(request.Email);

                var result = await _authenticationPort.LoginAsync(email, request.Password, request.TwoFactorCode, ct);

                if (result is not { IsSuccess: true, Value: not null })
                    return Result<AuthTokensDto>.Failure(result.Error ?? "Login failed");

                return Result<AuthTokensDto>.Success(AuthMapper.ToAuthTokensDto(result.Value));
            }
            catch (Exception ex)
            {
                return Result<AuthTokensDto>.Failure($"Login failed: {ex.Message}");
            }
        }
    }
}

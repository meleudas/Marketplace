using Marketplace.Application.Auth.DTOs;
using Marketplace.Application.Auth.Mappings;
using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.Entities;
using Marketplace.Domain.Users.Repositories;
using Marketplace.Domain.Users.ValueObjects;
using MediatR;

namespace Marketplace.Application.Auth.Commands.Register
{
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthTokensDto>>
    {
        private readonly IAuthenticationPort _authenticationPort;
        private readonly IUserRepository _userRepository;
        private readonly IEmailPort _emailPort;
        private readonly ITokenPort _tokenPort;

        public RegisterCommandHandler(
            IAuthenticationPort authenticationPort,
            IUserRepository userRepository,
            IEmailPort emailPort,
            ITokenPort tokenPort)
        {
            _authenticationPort = authenticationPort;
            _userRepository = userRepository;
            _emailPort = emailPort;
            _tokenPort = tokenPort;
        }

        public async Task<Result<AuthTokensDto>> Handle(RegisterCommand request, CancellationToken ct)
        {
            try
            {
                var email = AuthMapper.ToEmail(request.Email);
                var userName = AuthMapper.ToUserName(request.UserName);

                var identityId = IdentityUserId.New();
                var authResult = await _authenticationPort.RegisterAsync(
                    identityId,
                    email,
                    userName,
                    request.Password,
                    request.PhoneNumber,
                    ct
                );

                if (!authResult.IsSuccess)
                    return Result<AuthTokensDto>.Failure(authResult.Error ?? "Registration failed");

                var nameParts = request.UserName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var firstName = nameParts.Length > 0 ? nameParts[0] : request.UserName;
                var lastName = nameParts.Length > 1 ? string.Join(' ', nameParts.Skip(1)) : "-";
                var user = User.Create(identityId, firstName, lastName);
                await _userRepository.AddAsync(user, ct);

                var confirmToken = _tokenPort.GenerateEmailConfirmationToken(identityId, email.Value);
                if (_authenticationPort.RequireConfirmedEmail)
                {
                    await _emailPort.SendConfirmationEmailAsync(email.Value, confirmToken, ct);
                    return Result<AuthTokensDto>.Failure("Registration successful. Please confirm your email before login.");
                }

                _ = _emailPort.SendConfirmationEmailAsync(email.Value, confirmToken, ct);

                return Result<AuthTokensDto>.Success(AuthMapper.ToAuthTokensDto(authResult.Value!));
            }
            catch (Exception ex)
            {
                return Result<AuthTokensDto>.Failure($"Registration failed: {ex.Message}");
            }
        }
    }
}

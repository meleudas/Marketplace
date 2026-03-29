using Marketplace.Application.Auth.DTOs;
using Marketplace.Application.Auth.Mappings;
using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.Application.Auth.Commands.RefreshToken
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthTokensDto>>
    {
        private readonly IAuthenticationPort _authenticationPort;

        public RefreshTokenCommandHandler(IAuthenticationPort authenticationPort)
        {
            _authenticationPort = authenticationPort;
        }

        public async Task<Result<AuthTokensDto>> Handle(RefreshTokenCommand request, CancellationToken ct)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                    return Result<AuthTokensDto>.Failure("Refresh token is required.");

                var result = await _authenticationPort.RefreshTokenAsync(request.RefreshToken, ct);

                if (result is not { IsSuccess: true, Value: not null })
                    return Result<AuthTokensDto>.Failure(result.Error ?? "Token refresh failed");

                return Result<AuthTokensDto>.Success(AuthMapper.ToAuthTokensDto(result.Value));
            }
            catch (Exception ex)
            {
                return Result<AuthTokensDto>.Failure($"Token refresh failed: {ex.Message}");
            }
        }
    }
}

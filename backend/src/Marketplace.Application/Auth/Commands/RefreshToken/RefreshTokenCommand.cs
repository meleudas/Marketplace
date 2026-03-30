using Marketplace.Application.Auth.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;


namespace Marketplace.Application.Auth.Commands.RefreshToken
{
    public record RefreshTokenCommand(string? RefreshToken) : IRequest<Result<AuthTokensDto>>;
}

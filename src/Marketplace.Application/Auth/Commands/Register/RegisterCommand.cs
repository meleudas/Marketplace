using Marketplace.Application.Auth.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;


namespace Marketplace.Application.Auth.Commands.Register
{
    public record RegisterCommand(
     string Email,
     string Password,
     string UserName,
     string? PhoneNumber = null
 ) : IRequest<Result<AuthTokensDto>>;
}

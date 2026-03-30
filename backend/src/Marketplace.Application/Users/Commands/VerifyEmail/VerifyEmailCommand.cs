using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Users.Commands.VerifyEmail
{
    public record VerifyEmailCommand(
     string Email,
     string Token
 ) : IRequest<Result>;
}

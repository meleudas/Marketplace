using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Users.Commands.RequestPasswordReset
{
    public record RequestPasswordResetCommand(
        string Email
    ) : IRequest<Result>;
}

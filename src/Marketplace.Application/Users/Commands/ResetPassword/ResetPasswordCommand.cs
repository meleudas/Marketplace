using Marketplace.Domain.Shared.Kernel;
using MediatR;


namespace Marketplace.Application.Users.Commands.ResetPassword
{
    public record ResetPasswordCommand(
        string Email,
        string Token,
        string NewPassword
    ) : IRequest<Result>;
}

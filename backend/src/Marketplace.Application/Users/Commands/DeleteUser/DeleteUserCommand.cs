using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Users.Commands.DeleteUser;

/// <param name="IdentityUserId">Користувач, акаунт якого видаляється (має збігатися з JWT для self-delete).</param>
public record DeleteUserCommand(Guid IdentityUserId) : IRequest<Result>;

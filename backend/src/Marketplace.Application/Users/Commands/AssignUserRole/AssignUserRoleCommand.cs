using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Users.Enums;
using MediatR;

namespace Marketplace.Application.Users.Commands.AssignUserRole;

public sealed record AssignUserRoleCommand(Guid IdentityUserId, UserRole Role) : IRequest<Result>;

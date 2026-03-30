using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Auth.Commands.TwoFactor.EnableEmailTwoFactor;

public sealed record EnableEmailTwoFactorCommand(Guid IdentityUserId, string Code) : IRequest<Result>;


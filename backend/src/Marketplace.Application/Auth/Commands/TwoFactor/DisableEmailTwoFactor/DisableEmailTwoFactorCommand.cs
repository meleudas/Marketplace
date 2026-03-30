using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Auth.Commands.TwoFactor.DisableEmailTwoFactor;

public sealed record DisableEmailTwoFactorCommand(Guid IdentityUserId) : IRequest<Result>;


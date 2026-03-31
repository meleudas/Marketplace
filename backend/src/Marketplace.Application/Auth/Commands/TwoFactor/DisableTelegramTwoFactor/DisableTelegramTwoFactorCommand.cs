using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Auth.Commands.TwoFactor.DisableTelegramTwoFactor;

public sealed record DisableTelegramTwoFactorCommand(Guid IdentityUserId) : IRequest<Result>;

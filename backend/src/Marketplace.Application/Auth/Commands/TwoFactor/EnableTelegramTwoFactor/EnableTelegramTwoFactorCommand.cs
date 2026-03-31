using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Auth.Commands.TwoFactor.EnableTelegramTwoFactor;

public sealed record EnableTelegramTwoFactorCommand(Guid IdentityUserId, string Code) : IRequest<Result>;

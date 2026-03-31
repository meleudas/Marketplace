using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Auth.Commands.TwoFactor.SendTelegramTwoFactorCode;

public sealed record SendTelegramTwoFactorCodeCommand(Guid IdentityUserId) : IRequest<Result>;

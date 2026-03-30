using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Auth.Commands.TwoFactor.SendEmailTwoFactorCode;

public sealed record SendEmailTwoFactorCodeCommand(Guid IdentityUserId) : IRequest<Result>;


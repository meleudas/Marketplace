using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Auth.Commands.TwoFactor.GenerateTelegramLinkCode;

public sealed record GenerateTelegramLinkCodeCommand(Guid IdentityUserId) : IRequest<Result<string>>;

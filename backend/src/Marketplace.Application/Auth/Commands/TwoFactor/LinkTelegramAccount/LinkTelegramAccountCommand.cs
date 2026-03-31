using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Auth.Commands.TwoFactor.LinkTelegramAccount;

public sealed record LinkTelegramAccountCommand(string LinkCode, string ChatId) : IRequest<Result>;

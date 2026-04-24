using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Payments.Commands.HandleLiqPayWebhook;

public sealed record HandleLiqPayWebhookCommand(string Data, string Signature) : IRequest<Result>;

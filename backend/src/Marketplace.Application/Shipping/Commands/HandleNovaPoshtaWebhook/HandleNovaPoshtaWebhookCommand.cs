using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Shipping.Commands.HandleNovaPoshtaWebhook;

public sealed record HandleNovaPoshtaWebhookCommand(
    string EventKey,
    string PayloadHash,
    string RawPayload) : IRequest<Result>;

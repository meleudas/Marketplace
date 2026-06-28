using Marketplace.Application.Shipping.Services;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Shipping.Commands.HandleNovaPoshtaWebhook;

public sealed class HandleNovaPoshtaWebhookCommandHandler : IRequestHandler<HandleNovaPoshtaWebhookCommand, Result>
{
    private readonly IShipmentFulfillmentService _fulfillment;

    public HandleNovaPoshtaWebhookCommandHandler(IShipmentFulfillmentService fulfillment) =>
        _fulfillment = fulfillment;

    public async Task<Result> Handle(HandleNovaPoshtaWebhookCommand request, CancellationToken ct) =>
        await _fulfillment.ApplyCarrierEventAsync(
            Domain.Shipping.Enums.ShippingCarrierCode.NovaPoshta,
            request.EventKey,
            request.PayloadHash,
            request.RawPayload,
            ct);
}

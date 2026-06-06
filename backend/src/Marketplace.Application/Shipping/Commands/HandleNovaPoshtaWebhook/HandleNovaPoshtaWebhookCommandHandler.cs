using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Shipping.Entities;
using Marketplace.Domain.Shipping.Enums;
using Marketplace.Domain.Shipping.Repositories;
using MediatR;

namespace Marketplace.Application.Shipping.Commands.HandleNovaPoshtaWebhook;

public sealed class HandleNovaPoshtaWebhookCommandHandler : IRequestHandler<HandleNovaPoshtaWebhookCommand, Result>
{
    private readonly IShippingEventRepository _shippingEventRepository;

    public HandleNovaPoshtaWebhookCommandHandler(IShippingEventRepository shippingEventRepository)
    {
        _shippingEventRepository = shippingEventRepository;
    }

    public async Task<Result> Handle(HandleNovaPoshtaWebhookCommand request, CancellationToken ct)
    {
        try
        {
            var exists = await _shippingEventRepository.ExistsByDedupAsync(
                ShippingCarrierCode.NovaPoshta,
                request.EventKey,
                request.PayloadHash,
                ct);
            if (exists)
                return Result.Success();

            var now = DateTime.UtcNow;
            var evt = ShippingEvent.Reconstitute(
                ShippingEventId.From(0),
                ShippingCarrierCode.NovaPoshta,
                request.EventKey,
                request.PayloadHash,
                new JsonBlob(request.RawPayload),
                now,
                now,
                now,
                false,
                null);
            await _shippingEventRepository.AddAsync(evt, ct);
            return Result.Success();
        }
        catch
        {
            return Result.Failure("Failed to handle shipping webhook");
        }
    }
}

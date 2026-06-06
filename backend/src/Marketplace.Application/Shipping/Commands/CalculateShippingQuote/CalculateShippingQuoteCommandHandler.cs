using Marketplace.Application.Shipping.DTOs;
using Marketplace.Application.Shipping.Ports;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Shipping.Entities;
using Marketplace.Domain.Shipping.Repositories;
using MediatR;

namespace Marketplace.Application.Shipping.Commands.CalculateShippingQuote;

public sealed class CalculateShippingQuoteCommandHandler : IRequestHandler<CalculateShippingQuoteCommand, Result<ShippingQuoteDto>>
{
    private readonly IShippingMethodRepository _shippingMethodRepository;
    private readonly IShippingQuoteRepository _shippingQuoteRepository;
    private readonly INovaPoshtaPort _novaPoshtaPort;

    public CalculateShippingQuoteCommandHandler(
        IShippingMethodRepository shippingMethodRepository,
        IShippingQuoteRepository shippingQuoteRepository,
        INovaPoshtaPort novaPoshtaPort)
    {
        _shippingMethodRepository = shippingMethodRepository;
        _shippingQuoteRepository = shippingQuoteRepository;
        _novaPoshtaPort = novaPoshtaPort;
    }

    public async Task<Result<ShippingQuoteDto>> Handle(CalculateShippingQuoteCommand request, CancellationToken ct)
    {
        try
        {
            var method = await _shippingMethodRepository.GetByIdAsync(ShippingMethodId.From(request.ShippingMethodId), ct);
            if (method is null || !method.IsActive)
                return Result<ShippingQuoteDto>.Failure("Shipping method not found");

            var providerQuote = await _novaPoshtaPort.CalculateQuoteAsync(
                new NovaPoshtaQuoteRequest(
                    request.City,
                    request.State,
                    request.PostalCode,
                    request.Country,
                    method.Price.Amount),
                ct);

            if (!providerQuote.IsSuccess)
                return Result<ShippingQuoteDto>.Failure(providerQuote.Error ?? "Failed to calculate shipping quote");

            var now = DateTime.UtcNow;
            var expiresAtUtc = now.AddMinutes(30);
            var quote = ShippingQuote.Reconstitute(
                ShippingQuoteId.From(0),
                request.ActorUserId,
                method.Id,
                new Money(providerQuote.Amount),
                ContactPerson.Create(request.FirstName, request.LastName, request.Phone),
                Address.Create(request.Street, request.City, request.State, request.PostalCode, request.Country),
                expiresAtUtc,
                now,
                now,
                false,
                null);

            var saved = await _shippingQuoteRepository.AddAsync(quote, ct);
            return Result<ShippingQuoteDto>.Success(
                new ShippingQuoteDto(
                    saved.Id.Value,
                    saved.ShippingMethodId.Value,
                    saved.Amount.Amount,
                    providerQuote.EtaMinDays,
                    providerQuote.EtaMaxDays,
                    saved.ExpiresAtUtc));
        }
        catch
        {
            return Result<ShippingQuoteDto>.Failure("Failed to calculate shipping quote");
        }
    }
}

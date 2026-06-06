using Marketplace.Application.Shipping.DTOs;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Shipping.Repositories;
using MediatR;

namespace Marketplace.Application.Shipping.Queries.GetShippingMethods;

public sealed class GetShippingMethodsQueryHandler : IRequestHandler<GetShippingMethodsQuery, Result<IReadOnlyList<ShippingMethodDto>>>
{
    private readonly IShippingMethodRepository _shippingMethodRepository;

    public GetShippingMethodsQueryHandler(IShippingMethodRepository shippingMethodRepository)
    {
        _shippingMethodRepository = shippingMethodRepository;
    }

    public async Task<Result<IReadOnlyList<ShippingMethodDto>>> Handle(GetShippingMethodsQuery request, CancellationToken ct)
    {
        try
        {
            var rows = await _shippingMethodRepository.ListActiveAsync(ct);
            return Result<IReadOnlyList<ShippingMethodDto>>.Success(rows.Select(x => x.ToDto()).ToList());
        }
        catch
        {
            return Result<IReadOnlyList<ShippingMethodDto>>.Failure("Failed to get shipping methods");
        }
    }
}

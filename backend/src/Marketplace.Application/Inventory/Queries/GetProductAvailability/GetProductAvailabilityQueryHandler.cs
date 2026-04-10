using Marketplace.Application.Inventory.DTOs;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Inventory.Queries.GetProductAvailability;

public sealed class GetProductAvailabilityQueryHandler : IRequestHandler<GetProductAvailabilityQuery, Result<ProductAvailabilityDto>>
{
    private readonly IWarehouseStockRepository _stockRepository;

    public GetProductAvailabilityQueryHandler(IWarehouseStockRepository stockRepository)
    {
        _stockRepository = stockRepository;
    }

    public async Task<Result<ProductAvailabilityDto>> Handle(GetProductAvailabilityQuery request, CancellationToken ct)
    {
        try
        {
            var stocks = await _stockRepository.ListByProductAsync(CompanyId.From(request.CompanyId), ProductId.From(request.ProductId), ct);
            var available = stocks.Sum(x => x.Available);
            var status = available <= 0 ? "out_of_stock" : available <= 5 ? "low_stock" : "in_stock";
            return Result<ProductAvailabilityDto>.Success(new ProductAvailabilityDto(request.ProductId, available, status));
        }
        catch (Exception ex)
        {
            return Result<ProductAvailabilityDto>.Failure($"Failed to get product availability: {ex.Message}");
        }
    }
}

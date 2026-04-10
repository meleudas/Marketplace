using Marketplace.Application.Products.Authorization;
using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Mappings;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Queries.GetCompanyProducts;

public sealed class GetCompanyProductsQueryHandler : IRequestHandler<GetCompanyProductsQuery, Result<IReadOnlyList<ProductListItemDto>>>
{
    private readonly IProductAccessService _access;
    private readonly IProductRepository _productRepository;
    private readonly IWarehouseStockRepository _stockRepository;

    public GetCompanyProductsQueryHandler(IProductAccessService access, IProductRepository productRepository, IWarehouseStockRepository stockRepository)
    {
        _access = access;
        _productRepository = productRepository;
        _stockRepository = stockRepository;
    }

    public async Task<Result<IReadOnlyList<ProductListItemDto>>> Handle(GetCompanyProductsQuery request, CancellationToken ct)
    {
        try
        {
            if (!await _access.HasAccessAsync(request.CompanyId, request.ActorUserId, request.IsActorAdmin, ProductPermission.ReadInternal, ct))
                return Result<IReadOnlyList<ProductListItemDto>>.Failure("Forbidden");

            var companyId = CompanyId.From(request.CompanyId);
            var products = await _productRepository.ListByCompanyAsync(companyId, ct);
            var stocks = await _stockRepository.ListByCompanyAsync(companyId, ct);
            var availability = stocks.GroupBy(x => x.ProductId.Value).ToDictionary(g => g.Key, g => g.Sum(v => v.Available));

            var dtos = products.Select(p =>
            {
                var available = availability.GetValueOrDefault(p.Id.Value);
                var status = available <= 0 ? "out_of_stock" : available <= 5 ? "low_stock" : "in_stock";
                return ProductMapper.ToListItemDto(p, available, status);
            }).ToList();

            return Result<IReadOnlyList<ProductListItemDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<ProductListItemDto>>.Failure($"Failed to get company products: {ex.Message}");
        }
    }
}

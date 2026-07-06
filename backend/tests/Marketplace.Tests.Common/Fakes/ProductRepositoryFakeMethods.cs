using Marketplace.Application.Products.Catalog;
using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Ports;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Enums;
using Marketplace.Domain.Shared.Kernel;

namespace Marketplace.Tests.Common.Fakes;

public static class ProductRepositoryFakeMethods
{
    public static Task<IReadOnlyList<Product>> ListActiveOnSaleAsync(
        IEnumerable<Product> products,
        Guid? companyId = null,
        IReadOnlyList<long>? categoryIds = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        decimal? minDiscountPercent = null,
        CancellationToken ct = default)
    {
        _ = ct;
        var query = products
            .Where(x => x.Status == ProductStatus.Active && !x.IsDeleted)
            .Where(x => ProductDiscount.IsOnSale(x.Price.Amount, x.OldPrice?.Amount));

        if (companyId.HasValue)
            query = query.Where(x => x.CompanyId.Value == companyId.Value);

        if (categoryIds is { Count: > 0 })
        {
            var set = categoryIds.ToHashSet();
            query = query.Where(x => set.Contains(x.CategoryId.Value));
        }

        if (minPrice.HasValue)
            query = query.Where(x => x.Price.Amount >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(x => x.Price.Amount <= maxPrice.Value);

        if (minDiscountPercent.HasValue)
        {
            query = query.Where(x =>
            {
                var percent = ProductDiscount.Percent(x.Price.Amount, x.OldPrice?.Amount);
                return percent.HasValue && percent.Value >= minDiscountPercent.Value;
            });
        }

        return Task.FromResult<IReadOnlyList<Product>>(query.ToList());
    }

    public static Task<IReadOnlyList<Product>> ListActiveNewestAsync(
        IEnumerable<Product> products,
        Guid? companyId = null,
        IReadOnlyList<long>? categoryIds = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        CancellationToken ct = default)
    {
        _ = ct;
        var query = products
            .Where(x => x.Status == ProductStatus.Active && !x.IsDeleted);

        if (companyId.HasValue)
            query = query.Where(x => x.CompanyId.Value == companyId.Value);

        if (categoryIds is { Count: > 0 })
        {
            var set = categoryIds.ToHashSet();
            query = query.Where(x => set.Contains(x.CategoryId.Value));
        }

        if (minPrice.HasValue)
            query = query.Where(x => x.Price.Amount >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(x => x.Price.Amount <= maxPrice.Value);

        return Task.FromResult<IReadOnlyList<Product>>(query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id.Value)
            .ToList());
    }

    public static Task<IReadOnlyList<Product>> ListActivePopularAsync(
        IEnumerable<Product> products,
        Guid? companyId = null,
        IReadOnlyList<long>? categoryIds = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        CancellationToken ct = default)
    {
        _ = ct;
        var query = products
            .Where(x => x.Status == ProductStatus.Active && !x.IsDeleted);

        if (companyId.HasValue)
            query = query.Where(x => x.CompanyId.Value == companyId.Value);

        if (categoryIds is { Count: > 0 })
        {
            var set = categoryIds.ToHashSet();
            query = query.Where(x => set.Contains(x.CategoryId.Value));
        }

        if (minPrice.HasValue)
            query = query.Where(x => x.Price.Amount >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(x => x.Price.Amount <= maxPrice.Value);

        return Task.FromResult<IReadOnlyList<Product>>(query
            .OrderByDescending(x => x.SalesCount)
            .ThenByDescending(x => x.ViewCount)
            .ThenByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id.Value)
            .ToList());
    }
}

public static class ProductSearchServiceFakeMethods
{
    public static Task<Result<ProductSearchResultDto>> SearchCatalogOnSaleUnavailableAsync(
        CancellationToken ct = default)
    {
        _ = ct;
        return Task.FromResult(Result<ProductSearchResultDto>.Failure("search unavailable"));
    }

    public static Task<Result<ProductSearchResultDto>> SearchCatalogBrowsableUnavailableAsync(
        CancellationToken ct = default)
    {
        _ = ct;
        return Task.FromResult(Result<ProductSearchResultDto>.Failure("search unavailable"));
    }
}

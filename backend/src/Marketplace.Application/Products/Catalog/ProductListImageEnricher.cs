using Marketplace.Application.Products.DTOs;
using Marketplace.Domain.Catalog.Repositories;

namespace Marketplace.Application.Products.Catalog;

public static class ProductListImageEnricher
{
    public static async Task<IReadOnlyList<ProductListItemDto>> WithImageUrlsAsync(
        IReadOnlyList<ProductListItemDto> items,
        IProductImageRepository imageRepository,
        CancellationToken ct)
    {
        if (items.Count == 0)
            return items;

        var imageUrls = await imageRepository.ListImageUrlsByProductIdsAsync(items.Select(x => x.Id).ToArray(), ct);
        return items
            .Select(item => item with { ImageUrls = imageUrls.GetValueOrDefault(item.Id) ?? [] })
            .ToList();
    }
}

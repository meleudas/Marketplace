using Marketplace.Application.Products.DTOs;
using Marketplace.Domain.Catalog.Entities;

namespace Marketplace.Application.Products.Mappings;

public static class ProductMapper
{
    public static ProductListItemDto ToListItemDto(Product p, int availableQty, string availabilityStatus) =>
        new(
            p.Id.Value,
            p.CompanyId.Value,
            p.Name,
            p.Slug,
            p.Description,
            p.Price.Amount,
            p.OldPrice?.Amount,
            p.CategoryId.Value,
            p.Status.ToString(),
            p.HasVariants,
            p.Stock,
            p.MinStock,
            availableQty,
            availabilityStatus,
            p.CreatedAt,
            p.UpdatedAt);

    public static ProductDetailDto ToDetailDto(ProductDetail x) =>
        new(
            x.Slug,
            x.Attributes.Raw,
            x.Variants.Raw,
            x.Specifications.Raw,
            x.Seo.Raw,
            x.ContentBlocks.Raw,
            x.Tags,
            x.Brands);

    public static ProductImageDto ToImageDto(ProductImage x) =>
        new(
            x.ImageUrl,
            x.ThumbnailUrl,
            x.AltText,
            x.SortOrder,
            x.IsMain,
            x.Width,
            x.Height,
            x.FileSize);
}

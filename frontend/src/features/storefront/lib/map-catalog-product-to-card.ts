import type { CatalogProductListItemDto } from "@/features/storefront/model/catalog.types";
import type { ProductCardData } from "@/shared/ui/ProductCard";

export function mapCatalogProductToCardData(product: CatalogProductListItemDto): ProductCardData {
  return {
    id: String(product.id),
    title: product.name,
    price: product.price,
    oldPrice: product.oldPrice,
    discountPercent: product.discountPercent,
    imageUrl: product.imageUrls[0] ?? undefined,
    inStock: product.availabilityStatus !== "out_of_stock" && product.availableQty > 0,
  };
}

import type { CatalogProductListItemDto } from "@/features/storefront/model/catalog.types";
import type { ProductCardData } from "@/shared/ui";

export interface ProductRailCard extends ProductCardData {
  href: string;
}

export function mapProductToRailCard(product: CatalogProductListItemDto): ProductRailCard {
  return {
    id: String(product.id),
    imageUrl: product.imageUrls[0] ?? undefined,
    inStock: product.availabilityStatus !== "out_of_stock" && product.availableQty > 0,
    price: product.price,
    title: product.name,
    href: `/products/${product.slug}`,
  };
}

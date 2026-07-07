import type { CatalogProductListItemDto } from "@/features/storefront/model/catalog.types";
import { mapCatalogProductToCardData } from "@/features/storefront/lib/map-catalog-product-to-card";
import type { ProductCardData } from "@/shared/ui";

export interface ProductRailCard extends ProductCardData {
  href: string;
}

export function mapProductToRailCard(product: CatalogProductListItemDto): ProductRailCard {
  return {
    ...mapCatalogProductToCardData(product),
    href: `/products/${product.slug}`,
  };
}

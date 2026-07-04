import type { CatalogProductListItemDto } from "@/features/storefront/model/catalog.types";

/** Sort keys supported by `GET /catalog/products/search` on the backend. */
export type CatalogProductSort = "relevance" | "newest" | "price_asc" | "price_desc";

export const CATALOG_PRODUCT_SORT_OPTIONS: ReadonlyArray<{
  value: CatalogProductSort;
  label: string;
}> = [
  { value: "relevance", label: "За популярністю" },
  { value: "newest", label: "За новизною" },
  { value: "price_asc", label: "Від найдешевших" },
  { value: "price_desc", label: "Від найдорожчих" },
] as const;

export const DEFAULT_CATALOG_PRODUCT_SORT: CatalogProductSort = "relevance";

export const getCatalogProductSortLabel = (sort: CatalogProductSort): string =>
  CATALOG_PRODUCT_SORT_OPTIONS.find((option) => option.value === sort)?.label ?? "За популярністю";

/** Mirrors backend fallback sorting in SearchCatalogProductsQueryHandler. */
export const sortCatalogProducts = (
  items: CatalogProductListItemDto[],
  sort: CatalogProductSort,
): CatalogProductListItemDto[] => {
  const copy = [...items];

  switch (sort) {
    case "price_asc":
      return copy.sort((left, right) => left.price - right.price);
    case "price_desc":
      return copy.sort((left, right) => right.price - left.price);
    case "newest":
      return copy.sort(
        (left, right) => new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime(),
      );
    case "relevance":
    default:
      return copy.sort(
        (left, right) => new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime(),
      );
  }
};

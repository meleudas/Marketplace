import { useEffect, useMemo, useState } from "react";
import { parsePriceFilter } from "@/features/catalog/lib/catalog-filter-utils";
import {
  getCatalogProducts,
  searchCatalogProducts,
} from "@/features/storefront/api/catalog.api";
import {
  getCategoryFilterIds,
  getCategoryFilterIdsFromSlugs,
} from "@/features/storefront/lib/catalog-category-filter";
import {
  DEFAULT_CATALOG_PRODUCT_SORT,
  sortCatalogProducts,
  type CatalogProductSort,
} from "@/features/storefront/lib/catalog-product-sort";
import type { CatalogCategoryDto, CatalogProductListItemDto } from "@/features/storefront/model/catalog.types";

interface UseCatalogProductsParams {
  categories: CatalogCategoryDto[];
  selectedRootSlug: string | null;
  selectedSubcategorySlug: string | null;
  appliedAuthors: string[];
  appliedCategorySlugs: string[];
  appliedFormat: string | null;
  appliedMinPrice: string;
  appliedMaxPrice: string;
  searchQuery: string;
  selectedSort: CatalogProductSort;
}

interface UseCatalogProductsResult {
  products: CatalogProductListItemDto[];
  sortedProducts: CatalogProductListItemDto[];
  productsLoading: boolean;
  productsError: string | null;
}

export function useCatalogProducts({
  categories,
  selectedRootSlug,
  selectedSubcategorySlug,
  appliedAuthors,
  appliedCategorySlugs,
  appliedFormat,
  appliedMinPrice,
  appliedMaxPrice,
  searchQuery,
  selectedSort,
}: UseCatalogProductsParams): UseCatalogProductsResult {
  const [products, setProducts] = useState<CatalogProductListItemDto[]>([]);
  const [productsLoading, setProductsLoading] = useState(true);
  const [productsError, setProductsError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    const loadProducts = async () => {
      try {
        setProductsLoading(true);
        setProductsError(null);

        const routeCategory = selectedSubcategorySlug
          ? categories.find((category) => category.slug === selectedSubcategorySlug)
          : selectedRootSlug
            ? categories.find((category) => category.slug === selectedRootSlug)
            : null;
        const filterCategoryIds =
          appliedCategorySlugs.length > 0
            ? getCategoryFilterIdsFromSlugs(categories, appliedCategorySlugs)
            : undefined;
        const categoryFilterIds =
          filterCategoryIds && filterCategoryIds.length > 0
            ? filterCategoryIds
            : routeCategory
              ? getCategoryFilterIds(categories, routeCategory)
              : undefined;
        const minPrice = parsePriceFilter(appliedMinPrice);
        const maxPrice = parsePriceFilter(appliedMaxPrice);
        const shouldUseSearchEndpoint = Boolean(
          searchQuery ||
            routeCategory ||
            appliedCategorySlugs.length > 0 ||
            appliedAuthors.length > 0 ||
            appliedFormat ||
            typeof minPrice === "number" ||
            typeof maxPrice === "number" ||
            selectedSort !== DEFAULT_CATALOG_PRODUCT_SORT,
        );

        const nextProducts = shouldUseSearchEndpoint
          ? (
              await searchCatalogProducts({
                query: searchQuery || undefined,
                categoryIds: categoryFilterIds,
                minPrice,
                maxPrice,
                authors: appliedAuthors.length > 0 ? appliedAuthors : undefined,
                format: appliedFormat ?? undefined,
                sort: selectedSort !== DEFAULT_CATALOG_PRODUCT_SORT ? selectedSort : undefined,
                page: 1,
                pageSize: 100,
              })
            ).items
          : await getCatalogProducts();

        if (!cancelled) {
          setProducts(nextProducts);
        }
      } catch {
        if (!cancelled) {
          setProductsError("Не вдалося завантажити товари");
        }
      } finally {
        if (!cancelled) {
          setProductsLoading(false);
        }
      }
    };

    void loadProducts();

    return () => {
      cancelled = true;
    };
  }, [
    appliedAuthors,
    appliedCategorySlugs,
    appliedFormat,
    appliedMaxPrice,
    appliedMinPrice,
    categories,
    searchQuery,
    selectedRootSlug,
    selectedSubcategorySlug,
    selectedSort,
  ]);

  const sortedProducts = useMemo(() => {
    return sortCatalogProducts(products, selectedSort);
  }, [products, selectedSort]);

  return {
    products,
    sortedProducts,
    productsLoading,
    productsError,
  };
}

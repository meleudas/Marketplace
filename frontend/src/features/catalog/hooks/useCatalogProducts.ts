import { useEffect, useState } from "react";
import { parsePriceFilter } from "@/features/catalog/lib/catalog-filter-utils";
import { searchCatalogProducts } from "@/features/storefront/api/catalog.api";
import {
  getCategoryFilterIds,
  getCategoryFilterIdsFromSlugs,
} from "@/features/storefront/lib/catalog-category-filter";
import type { CatalogProductSort } from "@/features/storefront/lib/catalog-product-sort";
import type { CatalogCategoryDto, CatalogProductListItemDto } from "@/features/storefront/model/catalog.types";

interface UseCatalogProductsParams {
  categories: CatalogCategoryDto[];
  selectedRootSlug: string | null;
  selectedSubcategorySlug: string | null;
  appliedAuthors: string[];
  appliedCategorySlugs: string[];
  appliedFormat: string[];
  appliedMinPrice: string;
  appliedMaxPrice: string;
  searchQuery: string;
  selectedSort: CatalogProductSort;
  page: number;
  pageSize: number;
}

interface UseCatalogProductsResult {
  products: CatalogProductListItemDto[];
  totalProducts: number;
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
  page,
  pageSize,
}: UseCatalogProductsParams): UseCatalogProductsResult {
  const [products, setProducts] = useState<CatalogProductListItemDto[]>([]);
  const [totalProducts, setTotalProducts] = useState(0);
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
        const result = await searchCatalogProducts({
          query: searchQuery || undefined,
          categoryIds: categoryFilterIds,
          minPrice,
          maxPrice,
          authors: appliedAuthors.length > 0 ? appliedAuthors : undefined,
          format: appliedFormat.length > 0 ? appliedFormat.join(",") : undefined,
          sort: selectedSort,
          page,
          pageSize,
        });

        if (!cancelled) {
          setProducts(result.items);
          setTotalProducts(result.total);
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
    page,
    pageSize,
    searchQuery,
    selectedRootSlug,
    selectedSubcategorySlug,
    selectedSort,
  ]);

  return {
    products,
    totalProducts,
    productsLoading,
    productsError,
  };
}

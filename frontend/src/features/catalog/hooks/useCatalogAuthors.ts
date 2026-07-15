import { useEffect, useMemo, useState } from "react";
import { getCatalogAuthors } from "@/features/storefront/api/catalog.api";
import {
  getCategoryFilterIds,
  getCategoryFilterIdsFromSlugs,
} from "@/features/storefront/lib/catalog-category-filter";
import type { CatalogCategoryDto, CatalogFacetOptionDto } from "@/features/storefront/model/catalog.types";

interface UseCatalogAuthorsParams {
  categories: CatalogCategoryDto[];
  selectedRootSlug: string | null;
  selectedSubcategorySlug: string | null;
  appliedCategorySlugs: string[];
}

interface UseCatalogAuthorsResult {
  authors: CatalogFacetOptionDto[];
  authorsLoading: boolean;
  authorsError: string | null;
}

export function useCatalogAuthors({
  categories,
  selectedRootSlug,
  selectedSubcategorySlug,
  appliedCategorySlugs,
}: UseCatalogAuthorsParams): UseCatalogAuthorsResult {
  const [authors, setAuthors] = useState<CatalogFacetOptionDto[]>([]);
  const [authorsLoading, setAuthorsLoading] = useState(true);
  const [authorsError, setAuthorsError] = useState<string | null>(null);

  const categoryIds = useMemo(() => {
    const routeCategory = selectedSubcategorySlug
      ? categories.find((category) => category.slug === selectedSubcategorySlug)
      : selectedRootSlug
        ? categories.find((category) => category.slug === selectedRootSlug)
        : null;
    const filterCategoryIds =
      appliedCategorySlugs.length > 0
        ? getCategoryFilterIdsFromSlugs(categories, appliedCategorySlugs)
        : undefined;

    if (filterCategoryIds && filterCategoryIds.length > 0) {
      return filterCategoryIds;
    }

    return routeCategory ? getCategoryFilterIds(categories, routeCategory) : undefined;
  }, [appliedCategorySlugs, categories, selectedRootSlug, selectedSubcategorySlug]);

  const categoryIdsKey = useMemo(() => JSON.stringify(categoryIds ?? []), [categoryIds]);

  useEffect(() => {
    let cancelled = false;

    const loadAuthors = async () => {
      setAuthorsLoading(true);
      setAuthorsError(null);

      try {
        const nextAuthors = await getCatalogAuthors({
          categoryIds: categoryIdsKey === "[]" ? undefined : categoryIds,
        });

        if (!cancelled) {
          setAuthors(nextAuthors);
        }
      } catch {
        if (!cancelled) {
          setAuthors([]);
          setAuthorsError("Не вдалося завантажити авторів");
        }
      } finally {
        if (!cancelled) {
          setAuthorsLoading(false);
        }
      }
    };

    void loadAuthors();

    return () => {
      cancelled = true;
    };
  }, [categoryIds, categoryIdsKey]);

  return {
    authors,
    authorsLoading,
    authorsError,
  };
}

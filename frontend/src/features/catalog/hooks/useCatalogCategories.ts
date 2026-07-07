import { useEffect, useMemo, useState } from "react";
import { getCatalogCategories } from "@/features/storefront/api/catalog.api";
import {
  getChildCategories,
  getRootCategories,
  resolveCategorySelection,
} from "@/features/storefront/lib/catalog-category-filter";
import type { CatalogCategoryDto } from "@/features/storefront/model/catalog.types";

interface UseCatalogCategoriesResult {
  categories: CatalogCategoryDto[];
  rootCategories: CatalogCategoryDto[];
  visibleSubcategories: CatalogCategoryDto[];
  selectedRootSlug: string | null;
  selectedSubcategorySlug: string | null;
  categoryError: string | null;
}

export function useCatalogCategories(categorySlug?: string): UseCatalogCategoriesResult {
  const [categories, setCategories] = useState<CatalogCategoryDto[]>([]);
  const [categoryError, setCategoryError] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        setCategoryError(null);
        const categoriesData = await getCatalogCategories();

        setCategories(categoriesData.filter((category) => category.isActive));
      } catch {
        setCategoryError("Не вдалося завантажити категорії");
      }
    };

    void load();
  }, []);

  const rootCategories = useMemo(() => getRootCategories(categories), [categories]);

  const selection = useMemo(() => {
    if (!categorySlug || categories.length === 0) {
      return { rootSlug: null, subcategorySlug: null };
    }

    return resolveCategorySelection(categories, categorySlug);
  }, [categorySlug, categories]);

  const selectedRootSlug = selection.rootSlug;
  const selectedSubcategorySlug = selection.subcategorySlug;

  const selectedRootCategory = useMemo(
    () => rootCategories.find((category) => category.slug === selectedRootSlug) ?? null,
    [rootCategories, selectedRootSlug],
  );

  const visibleSubcategories = useMemo(() => {
    if (!selectedRootCategory) {
      return [];
    }

    return getChildCategories(categories, selectedRootCategory.id);
  }, [categories, selectedRootCategory]);

  return {
    categories,
    rootCategories,
    visibleSubcategories,
    selectedRootSlug,
    selectedSubcategorySlug,
    categoryError,
  };
}

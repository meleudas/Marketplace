import { useState } from "react";
import { toggleArrayFilter } from "@/features/catalog/lib/catalog-filter-utils";
import { getChildCategories, getRouteCategorySlugs } from "@/features/storefront/lib/catalog-category-filter";
import type { CatalogCategoryDto } from "@/features/storefront/model/catalog.types";

interface UseCatalogFiltersParams {
  categories: CatalogCategoryDto[];
  selectedRootSlug: string | null;
  selectedSubcategorySlug: string | null;
  onRouteCategoryMismatch: () => void;
  initialAuthors?: string[];
  initialCategorySlugs?: string[];
  initialFormat?: string[];
  initialMinPrice?: string;
  initialMaxPrice?: string;
  initialPageSize?: number;
}

export function useCatalogFilters({
  categories,
  selectedRootSlug,
  selectedSubcategorySlug,
  onRouteCategoryMismatch,
  initialAuthors,
  initialCategorySlugs,
  initialFormat,
  initialMinPrice,
  initialMaxPrice,
}: UseCatalogFiltersParams) {
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [appliedAuthors, setAppliedAuthors] = useState<string[]>(initialAuthors ?? []);
  const [appliedCategorySlugs, setAppliedCategorySlugs] = useState<string[]>(initialCategorySlugs ?? []);
  const [appliedFormat, setAppliedFormat] = useState<string[]>(initialFormat ?? []);
  const [appliedMinPrice, setAppliedMinPrice] = useState(initialMinPrice ?? "");
  const [appliedMaxPrice, setAppliedMaxPrice] = useState(initialMaxPrice ?? "");
  const [draftAuthors, setDraftAuthors] = useState<string[]>([]);
  const [draftCategorySlugs, setDraftCategorySlugs] = useState<string[]>([]);
  const [draftFormat, setDraftFormat] = useState<string[]>([]);
  const [draftMinPrice, setDraftMinPrice] = useState("");
  const [draftMaxPrice, setDraftMaxPrice] = useState("");
  const [showAllCategories, setShowAllCategories] = useState(false);
  const [showAllAuthors, setShowAllAuthors] = useState(false);

  const openFilters = () => {
    const routeCategorySlugs = getRouteCategorySlugs(selectedRootSlug, selectedSubcategorySlug);

    setDraftAuthors(appliedAuthors);
    setDraftCategorySlugs(
      appliedCategorySlugs.length > 0 ? appliedCategorySlugs : routeCategorySlugs,
    );
    setDraftFormat([...appliedFormat]);
    setDraftMinPrice(appliedMinPrice || "");
    setDraftMaxPrice(appliedMaxPrice || "");
    setShowAllCategories(false);
    setShowAllAuthors(false);
    setFiltersOpen(true);
  };

  const applyFilters = () => {
    setAppliedAuthors(draftAuthors);
    setAppliedCategorySlugs(draftCategorySlugs);
    setAppliedFormat(draftFormat);
    setAppliedMinPrice(draftMinPrice.trim());
    setAppliedMaxPrice(draftMaxPrice.trim());
    setFiltersOpen(false);

    const currentRouteCategorySlug = selectedSubcategorySlug ?? selectedRootSlug;
    const nextSingleCategorySlug = draftCategorySlugs.length === 1 ? draftCategorySlugs[0] : null;
    if (currentRouteCategorySlug && currentRouteCategorySlug !== nextSingleCategorySlug) {
      onRouteCategoryMismatch();
    }
  };

  const toggleDraftRootCategory = (slug: string) => {
    const category = categories.find((item) => item.slug === slug);
    const childSlugs = category
      ? getChildCategories(categories, category.id).map((child) => child.slug)
      : [];

    setDraftCategorySlugs((current) => {
      const withoutChildren = current.filter((value) => !childSlugs.includes(value));
      return toggleArrayFilter(withoutChildren, slug);
    });
  };

  const toggleDraftSubcategory = (rootSlug: string, subcategorySlug: string) => {
    setDraftCategorySlugs((current) => {
      const withoutRoot = current.filter((value) => value !== rootSlug);
      return toggleArrayFilter(withoutRoot, subcategorySlug);
    });
  };

  const removeAppliedAuthor = (author: string) => {
    setAppliedAuthors((current) => current.filter((value) => value !== author));
  };

  const removeAppliedCategorySlug = (slug: string) => {
    setAppliedCategorySlugs((current) => current.filter((value) => value !== slug));
  };

  const removeAppliedFormat = (format: string) => {
    setAppliedFormat((current) => current.filter((value) => value !== format));
  };

  const notifyRouteMismatch = (nextCategorySlugs: string[]) => {
    const currentRouteCategorySlug = selectedSubcategorySlug ?? selectedRootSlug;
    const nextSingleCategorySlug = nextCategorySlugs.length === 1 ? nextCategorySlugs[0] : null;
    if (currentRouteCategorySlug && currentRouteCategorySlug !== nextSingleCategorySlug) {
      onRouteCategoryMismatch();
    }
  };

  /** Instant-apply toggles for the always-visible desktop sidebar (no draft/apply step). */
  const toggleAppliedRootCategory = (slug: string) => {
    const category = categories.find((item) => item.slug === slug);
    const childSlugs = category
      ? getChildCategories(categories, category.id).map((child) => child.slug)
      : [];

    const withoutChildren = appliedCategorySlugs.filter((value) => !childSlugs.includes(value));
    const next = toggleArrayFilter(withoutChildren, slug);
    setAppliedCategorySlugs(next);
    notifyRouteMismatch(next);
  };

  const toggleAppliedSubcategory = (rootSlug: string, subcategorySlug: string) => {
    const withoutRoot = appliedCategorySlugs.filter((value) => value !== rootSlug);
    const next = toggleArrayFilter(withoutRoot, subcategorySlug);
    setAppliedCategorySlugs(next);
    notifyRouteMismatch(next);
  };

  const toggleAppliedAuthor = (author: string) => {
    setAppliedAuthors((current) => toggleArrayFilter(current, author));
  };

  const toggleAppliedFormatValue = (format: string) => {
    setAppliedFormat((current) => toggleArrayFilter(current, format));
  };

  const applyAppliedPriceRange = (minPrice: string, maxPrice: string) => {
    setAppliedMinPrice(minPrice.trim());
    setAppliedMaxPrice(maxPrice.trim());
  };

  const resetAppliedFilters = () => {
    setAppliedAuthors([]);
    setAppliedCategorySlugs([]);
    setAppliedFormat([]);
    setAppliedMinPrice("");
    setAppliedMaxPrice("");
  };

  return {
    filtersOpen,
    setFiltersOpen,
    appliedAuthors,
    setAppliedAuthors,
    appliedCategorySlugs,
    setAppliedCategorySlugs,
    appliedFormat,
    setAppliedFormat,
    appliedMinPrice,
    appliedMaxPrice,
    draftAuthors,
    setDraftAuthors,
    draftCategorySlugs,
    draftFormat,
    setDraftFormat,
    draftMinPrice,
    setDraftMinPrice,
    draftMaxPrice,
    setDraftMaxPrice,
    showAllCategories,
    setShowAllCategories,
    showAllAuthors,
    setShowAllAuthors,
    openFilters,
    applyFilters,
    toggleDraftRootCategory,
    toggleDraftSubcategory,
    removeAppliedAuthor,
    removeAppliedCategorySlug,
    removeAppliedFormat,
    toggleAppliedRootCategory,
    toggleAppliedSubcategory,
    toggleAppliedAuthor,
    toggleAppliedFormatValue,
    applyAppliedPriceRange,
    resetAppliedFilters,
  };
}

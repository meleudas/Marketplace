import { useState } from "react";
import {
  DEFAULT_CATALOG_MAX_PRICE,
  DEFAULT_CATALOG_MIN_PRICE,
  resolveAppliedPriceFilter,
} from "@/features/catalog/lib/catalog-filter-options";
import { toggleArrayFilter, toggleSingleFilter } from "@/features/catalog/lib/catalog-filter-utils";
import { getChildCategories, getRouteCategorySlugs } from "@/features/storefront/lib/catalog-category-filter";
import type { CatalogCategoryDto } from "@/features/storefront/model/catalog.types";

interface UseCatalogFiltersParams {
  categories: CatalogCategoryDto[];
  selectedRootSlug: string | null;
  selectedSubcategorySlug: string | null;
  onRouteCategoryMismatch: () => void;
}

export function useCatalogFilters({
  categories,
  selectedRootSlug,
  selectedSubcategorySlug,
  onRouteCategoryMismatch,
}: UseCatalogFiltersParams) {
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [appliedAuthors, setAppliedAuthors] = useState<string[]>([]);
  const [appliedCategorySlugs, setAppliedCategorySlugs] = useState<string[]>([]);
  const [appliedFormat, setAppliedFormat] = useState<string | null>(null);
  const [appliedMinPrice, setAppliedMinPrice] = useState("");
  const [appliedMaxPrice, setAppliedMaxPrice] = useState("");
  const [draftAuthors, setDraftAuthors] = useState<string[]>([]);
  const [draftCategorySlugs, setDraftCategorySlugs] = useState<string[]>([]);
  const [draftFormat, setDraftFormat] = useState<string | null>(null);
  const [draftMinPrice, setDraftMinPrice] = useState(DEFAULT_CATALOG_MIN_PRICE);
  const [draftMaxPrice, setDraftMaxPrice] = useState(DEFAULT_CATALOG_MAX_PRICE);
  const [showAllCategories, setShowAllCategories] = useState(false);
  const [showAllAuthors, setShowAllAuthors] = useState(false);

  const openFilters = () => {
    const routeCategorySlugs = getRouteCategorySlugs(selectedRootSlug, selectedSubcategorySlug);

    setDraftAuthors(appliedAuthors);
    setDraftCategorySlugs(
      appliedCategorySlugs.length > 0 ? appliedCategorySlugs : routeCategorySlugs,
    );
    setDraftFormat(appliedFormat);
    setDraftMinPrice(appliedMinPrice || DEFAULT_CATALOG_MIN_PRICE);
    setDraftMaxPrice(appliedMaxPrice || DEFAULT_CATALOG_MAX_PRICE);
    setShowAllCategories(false);
    setShowAllAuthors(false);
    setFiltersOpen(true);
  };

  const applyFilters = () => {
    setAppliedAuthors(draftAuthors);
    setAppliedCategorySlugs(draftCategorySlugs);
    setAppliedFormat(draftFormat);
    setAppliedMinPrice(resolveAppliedPriceFilter(draftMinPrice, DEFAULT_CATALOG_MIN_PRICE));
    setAppliedMaxPrice(resolveAppliedPriceFilter(draftMaxPrice, DEFAULT_CATALOG_MAX_PRICE));
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

  const removeAppliedFormat = () => {
    setAppliedFormat(null);
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
    setAppliedFormat((current) => toggleSingleFilter(current, format));
  };

  const applyAppliedPriceRange = (minPrice: string, maxPrice: string) => {
    setAppliedMinPrice(resolveAppliedPriceFilter(minPrice, DEFAULT_CATALOG_MIN_PRICE));
    setAppliedMaxPrice(resolveAppliedPriceFilter(maxPrice, DEFAULT_CATALOG_MAX_PRICE));
  };

  const resetAppliedFilters = () => {
    setAppliedAuthors([]);
    setAppliedCategorySlugs([]);
    setAppliedFormat(null);
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

"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import { useCatalogCategories } from "@/features/catalog/hooks/useCatalogCategories";
import { useCatalogFilters } from "@/features/catalog/hooks/useCatalogFilters";
import { useCatalogProducts } from "@/features/catalog/hooks/useCatalogProducts";
import { isCatalogProductFormat } from "@/features/catalog/lib/catalog-filter-options";
import { CatalogFilterPanel } from "@/features/catalog/ui/CatalogFilterPanel";
import { CatalogProductGrid } from "@/features/catalog/ui/CatalogProductGrid";
import { CatalogSelectedFilters } from "@/features/catalog/ui/CatalogSelectedFilters";
import { CatalogSortSheet } from "@/features/catalog/ui/CatalogSortSheet";
import { CatalogToolbar } from "@/features/catalog/ui/CatalogToolbar";
import {
  DEFAULT_CATALOG_PRODUCT_SORT,
  getCatalogProductSortLabel,
  type CatalogProductSort,
} from "@/features/storefront/lib/catalog-product-sort";
import { StateBlock } from "@/features/storefront/ui/StateBlock";
import { CatalogMenu, PageLayout, type CatalogFormatFilter } from "@/shared/ui";
import styles from "./CatalogScreen.module.css";

interface CatalogScreenProps {
  categorySlug?: string;
}

const PAGE_SIZE = 8;

export function CatalogScreen({ categorySlug }: CatalogScreenProps) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const {
    categories,
    rootCategories,
    selectedRootSlug,
    selectedSubcategorySlug,
    categoryError,
  } = useCatalogCategories(categorySlug);
  const [selectedSort, setSelectedSort] = useState<CatalogProductSort>(DEFAULT_CATALOG_PRODUCT_SORT);
  const [sortModalOpen, setSortModalOpen] = useState(false);
  const [catalogOpen, setCatalogOpen] = useState(false);
  const [searchInput, setSearchInput] = useState("");
  const [searchQuery, setSearchQuery] = useState("");
  const [pageState, setPageState] = useState({ key: "", page: 1 });
  const pendingScrollY = useRef<number | null>(null);

  const filters = useCatalogFilters({
    categories,
    selectedRootSlug,
    selectedSubcategorySlug,
    onRouteCategoryMismatch: () => router.push("/catalog"),
  });

  const {
    filtersOpen,
    setFiltersOpen,
    appliedAuthors,
    appliedCategorySlugs,
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
  } = filters;

  const { products, sortedProducts, productsLoading, productsError } = useCatalogProducts({
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
  });

  useEffect(() => {
    const formatParam = searchParams.get("format");

    if (formatParam && isCatalogProductFormat(formatParam)) {
      setAppliedFormat(formatParam);
    }
  }, [searchParams, setAppliedFormat]);

  useEffect(() => {
    const sortParam = searchParams.get("sort");
    if (
      sortParam === "relevance" ||
      sortParam === "newest" ||
      sortParam === "price_asc" ||
      sortParam === "price_desc"
    ) {
      setSelectedSort(sortParam);
    }
  }, [searchParams]);

  const paginationKey = useMemo(() => {
    return JSON.stringify({
      appliedAuthors,
      appliedCategorySlugs,
      appliedFormat,
      appliedMaxPrice,
      appliedMinPrice,
      selectedRootSlug,
      selectedSubcategorySlug,
      selectedSort,
      searchQuery,
    });
  }, [
    appliedAuthors,
    appliedCategorySlugs,
    appliedFormat,
    appliedMaxPrice,
    appliedMinPrice,
    selectedRootSlug,
    selectedSubcategorySlug,
    selectedSort,
    searchQuery,
  ]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setSearchQuery(searchInput.trim());
    }, 350);

    return () => window.clearTimeout(timeoutId);
  }, [searchInput]);

  const isInitialLoading = productsLoading && products.length === 0;
  const error = categoryError ?? productsError;
  const totalPages = Math.max(1, Math.ceil(sortedProducts.length / PAGE_SIZE));
  const requestedPage = pageState.key === paginationKey ? pageState.page : 1;
  const currentPage = Math.min(requestedPage, totalPages);

  const paginatedProducts = sortedProducts.slice(
    (currentPage - 1) * PAGE_SIZE,
    currentPage * PAGE_SIZE,
  );

  useLayoutEffect(() => {
    if (pendingScrollY.current === null) {
      return;
    }

    window.scrollTo(0, pendingScrollY.current);
    pendingScrollY.current = null;
  }, [currentPage, paginatedProducts]);

  useEffect(() => {
    if (!sortModalOpen && !filtersOpen) {
      return;
    }

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    return () => {
      document.body.style.overflow = previousOverflow;
    };
  }, [filtersOpen, sortModalOpen]);

  const sortButtonLabel = getCatalogProductSortLabel(selectedSort);

  const handlePageChange = (page: number) => {
    if (page === currentPage) {
      return;
    }

    pendingScrollY.current = window.scrollY;
    setPageState({ key: paginationKey, page });
  };

  const handleSortSelect = (sort: CatalogProductSort) => {
    setSelectedSort(sort);
    setSortModalOpen(false);
  };

  const handleOpenFilters = () => {
    setSortModalOpen(false);
    openFilters();
  };

  const handleCatalogCategorySelect = (slug: string, format: CatalogFormatFilter) => {
    setAppliedFormat(format === "all" ? null : format);
    const formatParam = format === "all" ? "" : `?format=${encodeURIComponent(format)}`;
    router.push(`/catalog/${slug}${formatParam}`);
  };

  const handleShowAllCategories = (format: CatalogFormatFilter) => {
    setAppliedFormat(format === "all" ? null : format);
    const formatParam = format === "all" ? "" : `?format=${encodeURIComponent(format)}`;
    router.push(`/catalog${formatParam}`);
  };

  const catalogMenuFormat: CatalogFormatFilter =
    appliedFormat && isCatalogProductFormat(appliedFormat) ? appliedFormat : "all";

  const handleRemoveCategory = (slug: string, source: "applied" | "route") => {
    if (source === "applied") {
      removeAppliedCategorySlug(slug);
      return;
    }

    if (selectedSubcategorySlug === slug) {
      router.push(selectedRootSlug ? `/catalog/${selectedRootSlug}` : "/catalog");
      return;
    }

    if (selectedRootSlug === slug) {
      router.push("/catalog");
    }
  };

  const handleRemoveFormat = () => {
    removeAppliedFormat();

    if (searchParams.get("format")) {
      router.push(categorySlug ? `/catalog/${categorySlug}` : "/catalog");
    }
  };

  return (
    <PageLayout
      headerProps={{
        homeHref: "/",
        userHref: "/me",
        searchPlaceholder: "Пошук книг",
        onSearchChange: setSearchInput,
        onMenuClick: () => setCatalogOpen(true),
      }}
      footerProps={{ homeHref: "/" }}
    >
      <h1 className={styles.pageTitle}>Каталог</h1>

      <CatalogSelectedFilters
        loading={isInitialLoading}
        categories={categories}
        routeCategorySlugs={[selectedRootSlug, selectedSubcategorySlug].filter(
          (slug): slug is string => Boolean(slug),
        )}
        appliedCategorySlugs={appliedCategorySlugs}
        appliedAuthors={appliedAuthors}
        appliedFormat={appliedFormat}
        onRemoveCategory={handleRemoveCategory}
        onRemoveAuthor={removeAppliedAuthor}
        onRemoveFormat={handleRemoveFormat}
      />

      <CatalogToolbar
        loading={isInitialLoading}
        filtersOpen={filtersOpen}
        sortModalOpen={sortModalOpen}
        sortButtonLabel={sortButtonLabel}
        onOpenFilters={handleOpenFilters}
        onToggleSort={() => setSortModalOpen((open) => !open)}
      />

      <CatalogFilterPanel
        open={filtersOpen && !isInitialLoading}
        rootCategories={rootCategories}
        categories={categories}
        draftAuthors={draftAuthors}
        draftCategorySlugs={draftCategorySlugs}
        draftFormat={draftFormat}
        draftMinPrice={draftMinPrice}
        draftMaxPrice={draftMaxPrice}
        showAllCategories={showAllCategories}
        showAllAuthors={showAllAuthors}
        onClose={() => setFiltersOpen(false)}
        onApply={applyFilters}
        onDraftAuthorsChange={setDraftAuthors}
        onDraftRootCategoryToggle={toggleDraftRootCategory}
        onDraftSubcategoryToggle={toggleDraftSubcategory}
        onDraftFormatChange={setDraftFormat}
        onDraftMinPriceChange={setDraftMinPrice}
        onDraftMaxPriceChange={setDraftMaxPrice}
        onShowAllCategoriesChange={setShowAllCategories}
        onShowAllAuthorsChange={setShowAllAuthors}
      />

      {error ? <StateBlock message={error} isError /> : null}

      <CatalogProductGrid
        loading={isInitialLoading}
        refreshing={productsLoading && !isInitialLoading}
        error={error}
        products={paginatedProducts}
        totalPages={totalPages}
        currentPage={currentPage}
        onPageChange={handlePageChange}
      />

      <CatalogSortSheet
        open={sortModalOpen}
        selectedSort={selectedSort}
        onClose={() => setSortModalOpen(false)}
        onSelect={handleSortSelect}
      />

      <CatalogMenu
        open={catalogOpen}
        categories={categories.map((category) => ({
          id: category.id,
          name: category.name,
          slug: category.slug,
          parentId: category.parentId,
          sortOrder: category.sortOrder,
        }))}
        onClose={() => setCatalogOpen(false)}
        onCategorySelect={handleCatalogCategorySelect}
        onShowAll={handleShowAllCategories}
        activeFormat={catalogMenuFormat}
      />
    </PageLayout>
  );
}

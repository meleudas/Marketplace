"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import { useCatalogAuthors } from "@/features/catalog/hooks/useCatalogAuthors";
import { useCatalogCategories } from "@/features/catalog/hooks/useCatalogCategories";
import { useCatalogPageSize } from "@/features/catalog/hooks/useCatalogPageSize";
import { useCatalogFilters } from "@/features/catalog/hooks/useCatalogFilters";
import { useCatalogProducts } from "@/features/catalog/hooks/useCatalogProducts";
import { isCatalogProductFormat } from "@/features/catalog/lib/catalog-filter-options";
import { CatalogFilterPanel } from "@/features/catalog/ui/CatalogFilterPanel";
import { CatalogFilterSidebar } from "@/features/catalog/ui/CatalogFilterSidebar";
import { CatalogProductGrid } from "@/features/catalog/ui/CatalogProductGrid";
import { CatalogSelectedFilters } from "@/features/catalog/ui/CatalogSelectedFilters";
import { CatalogSortSheet } from "@/features/catalog/ui/CatalogSortSheet";
import { CatalogToolbar } from "@/features/catalog/ui/CatalogToolbar";
import {
  DEFAULT_CATALOG_PRODUCT_SORT,
  getCatalogProductSortLabel,
  type CatalogProductSort,
} from "@/features/storefront/lib/catalog-product-sort";
import { getRouteCategorySlugs } from "@/features/storefront/lib/catalog-category-filter";
import { StateBlock } from "@/features/storefront/ui/StateBlock";
import { PageLayout, type CatalogFormatFilter } from "@/shared/ui";
import styles from "./CatalogScreen.module.css";

interface CatalogScreenProps {
  categorySlug?: string;
}

function parseCatalogSortParam(value: string | null): CatalogProductSort | null {
  if (
    value === "relevance" ||
    value === "newest" ||
    value === "price_asc" ||
    value === "price_desc"
  ) {
    return value;
  }

  return null;
}

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
  const [manualSort, setManualSort] = useState<CatalogProductSort>(DEFAULT_CATALOG_PRODUCT_SORT);
  const sortFromUrl = useMemo(
    () => parseCatalogSortParam(searchParams.get("sort")),
    [searchParams],
  );
  const selectedSort = sortFromUrl ?? manualSort;
  const [sortModalOpen, setSortModalOpen] = useState(false);
  const [searchInput, setSearchInput] = useState("");
  const [searchQuery, setSearchQuery] = useState("");
  const [pageState, setPageState] = useState({ key: "", page: 1 });
  const pendingScrollY = useRef<number | null>(null);
  const pageSize = useCatalogPageSize();

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
    toggleAppliedRootCategory,
    toggleAppliedSubcategory,
    toggleAppliedAuthor,
    toggleAppliedFormatValue,
    applyAppliedPriceRange,
    resetAppliedFilters,
  } = filters;

  const { authors, authorsLoading } = useCatalogAuthors({
    categories,
    selectedRootSlug,
    selectedSubcategorySlug,
    appliedCategorySlugs,
  });

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
      pageSize,
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
    pageSize,
  ]);
  const requestedPage = pageState.key === paginationKey ? pageState.page : 1;

  const { products, totalProducts, productsLoading, productsError } = useCatalogProducts({
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
    page: requestedPage,
    pageSize,
  });

  useEffect(() => {
    const formatParam = searchParams.get("format");

    if (formatParam && isCatalogProductFormat(formatParam)) {
      setAppliedFormat(formatParam);
      return;
    }

    setAppliedFormat(null);
  }, [searchParams, setAppliedFormat]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setSearchQuery(searchInput.trim());
    }, 350);

    return () => window.clearTimeout(timeoutId);
  }, [searchInput]);

  const isInitialLoading = productsLoading && products.length === 0;
  const error = categoryError ?? productsError;
  const totalPages = Math.max(1, Math.ceil(totalProducts / pageSize));
  const currentPage = Math.min(requestedPage, totalPages);

  useLayoutEffect(() => {
    if (pendingScrollY.current === null) {
      return;
    }

    window.scrollTo(0, pendingScrollY.current);
    pendingScrollY.current = null;
  }, [currentPage, products]);

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
    setManualSort(sort);
    setSortModalOpen(false);
  };

  const handleOpenFilters = () => {
    setSortModalOpen(false);
    openFilters();
  };

  const catalogMenuFormat: CatalogFormatFilter =
    appliedFormat && isCatalogProductFormat(appliedFormat) ? appliedFormat : "all";

  const handleRemoveCategory = (slug: string, source: "applied" | "route") => {
    if (source === "applied") {
      removeAppliedCategorySlug(slug);
      return;
    }

    router.push("/catalog");
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
        catalogActiveFormat: catalogMenuFormat,
      }}
      footerProps={{ homeHref: "/" }}
    >
      <h1 className={styles.pageTitle}>Каталог</h1>

      <div className={styles.catalogLayout}>
        <CatalogFilterSidebar
          rootCategories={rootCategories}
          categories={categories}
          authorOptions={authors}
          authorsLoading={authorsLoading}
          appliedCategorySlugs={appliedCategorySlugs}
          appliedAuthors={appliedAuthors}
          appliedFormat={appliedFormat}
          appliedMinPrice={appliedMinPrice}
          appliedMaxPrice={appliedMaxPrice}
          onToggleRootCategory={toggleAppliedRootCategory}
          onToggleSubcategory={toggleAppliedSubcategory}
          onToggleAuthor={toggleAppliedAuthor}
          onToggleFormat={toggleAppliedFormatValue}
          onApplyPriceRange={applyAppliedPriceRange}
          onReset={resetAppliedFilters}
        />

        <div className={styles.catalogMain}>
          <CatalogSelectedFilters
            loading={isInitialLoading}
            categories={categories}
            authorOptions={authors}
            routeCategorySlugs={getRouteCategorySlugs(selectedRootSlug, selectedSubcategorySlug)}
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
            totalCount={totalProducts}
            selectedSort={selectedSort}
            onOpenFilters={handleOpenFilters}
            onToggleSort={() => setSortModalOpen((open) => !open)}
            onSelectSort={handleSortSelect}
          />

          <CatalogFilterPanel
            open={filtersOpen && !isInitialLoading}
            rootCategories={rootCategories}
            categories={categories}
            authorOptions={authors}
            authorsLoading={authorsLoading}
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
            products={products}
            pageSize={pageSize}
            totalPages={totalPages}
            currentPage={currentPage}
            onPageChange={handlePageChange}
          />
        </div>
      </div>

      <CatalogSortSheet
        open={sortModalOpen}
        selectedSort={selectedSort}
        onClose={() => setSortModalOpen(false)}
        onSelect={handleSortSelect}
      />
    </PageLayout>
  );
}

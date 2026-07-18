"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import { useCatalogAuthors } from "@/features/catalog/hooks/useCatalogAuthors";
import { useCatalogCategories } from "@/features/catalog/hooks/useCatalogCategories";
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

function parseUrlParams(searchParams: URLSearchParams) {
  const page = Math.max(1, parseInt(searchParams.get("page") ?? "", 10) || 1);
  const q = searchParams.get("q") ?? "";
  const sort = parseCatalogSortParam(searchParams.get("sort"));
  const format = searchParams.get("format")?.split(",").filter(Boolean) ?? [];
  const authors = searchParams.get("authors")?.split(",").filter(Boolean) ?? [];
  const categories = searchParams.get("categories")?.split(",").filter(Boolean) ?? [];
  const minPrice = searchParams.get("minPrice") ?? "";
  const maxPrice = searchParams.get("maxPrice") ?? "";
  const perPage = parseInt(searchParams.get("perPage") ?? "", 10);
  const pageSize = [10, 20, 30, 40, 50].includes(perPage) ? perPage : 20;

  return { page, q, sort, format, authors, categories, minPrice, maxPrice, pageSize };
}

export function CatalogScreen({ categorySlug }: CatalogScreenProps) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const urlParams = useMemo(() => parseUrlParams(searchParams), [searchParams]);

  const {
    categories,
    rootCategories,
    selectedRootSlug,
    selectedSubcategorySlug,
    categoryError,
  } = useCatalogCategories(categorySlug);
  const [manualSort, setManualSort] = useState<CatalogProductSort>(urlParams.sort ?? DEFAULT_CATALOG_PRODUCT_SORT);
  const selectedSort = urlParams.sort ?? manualSort;
  const [sortModalOpen, setSortModalOpen] = useState(false);
  const [searchInput, setSearchInput] = useState(urlParams.q);
  const [searchQuery, setSearchQuery] = useState(urlParams.q);
  const [page, setPage] = useState(urlParams.page);
  const pendingScrollY = useRef<number | null>(null);
  const [pageSize, setPageSize] = useState(urlParams.pageSize);
  const initialUrlRef = useRef(false);
  const filters = useCatalogFilters({
    categories,
    selectedRootSlug,
    selectedSubcategorySlug,
    onRouteCategoryMismatch: () => router.push("/catalog"),
    initialAuthors: urlParams.authors,
    initialCategorySlugs: urlParams.categories,
    initialFormat: urlParams.format,
    initialMinPrice: urlParams.minPrice,
    initialMaxPrice: urlParams.maxPrice,
  });

  const {
    filtersOpen,
    setFiltersOpen,
    appliedAuthors,
    appliedCategorySlugs,
    appliedFormat,
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

  const requestedPage = page;

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
    const timeoutId = window.setTimeout(() => {
      setSearchQuery(searchInput.trim());
    }, 350);

    return () => window.clearTimeout(timeoutId);
  }, [searchInput]);

  const isInitialLoading = productsLoading && products.length === 0;
  const error = categoryError ?? productsError;
  const totalPages = Math.max(1, Math.ceil(totalProducts / pageSize));
  const currentPage = Math.min(page, totalPages);

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

  useEffect(() => {
    initialUrlRef.current = true;
  }, []);

  // Reset to page 1 when filters or search change
  useEffect(() => {
    if (!initialUrlRef.current) return;
    setPage(1);
  }, [appliedAuthors, appliedCategorySlugs, appliedFormat, appliedMinPrice, appliedMaxPrice, searchQuery]);

  useEffect(() => {
    if (!initialUrlRef.current) return;

    const params = new URLSearchParams();

    if (appliedAuthors.length > 0) {
      params.set("authors", appliedAuthors.join(","));
    }
    if (appliedCategorySlugs.length > 0) {
      params.set("categories", appliedCategorySlugs.join(","));
    }
    if (appliedFormat.length > 0) {
      params.set("format", appliedFormat.join(","));
    }
    if (appliedMinPrice) {
      params.set("minPrice", appliedMinPrice);
    }
    if (appliedMaxPrice) {
      params.set("maxPrice", appliedMaxPrice);
    }
    if (page > 1) {
      params.set("page", String(page));
    }
    if (searchQuery) {
      params.set("q", searchQuery);
    }
    if (selectedSort !== DEFAULT_CATALOG_PRODUCT_SORT) {
      params.set("sort", selectedSort);
    }
    if (pageSize !== 20) {
      params.set("perPage", String(pageSize));
    }

    const qs = params.toString();
    const basePath = categorySlug ? `/catalog/${categorySlug}` : "/catalog";
    router.replace(qs ? `${basePath}?${qs}` : basePath, { scroll: false });
  }, [
    appliedAuthors,
    appliedCategorySlugs,
    appliedFormat,
    appliedMinPrice,
    appliedMaxPrice,
    page,
    searchQuery,
    selectedSort,
    pageSize,
    categorySlug,
    router,
  ]);

  const sortButtonLabel = getCatalogProductSortLabel(selectedSort);

  const handlePageChange = (nextPage: number) => {
    if (nextPage === currentPage) {
      return;
    }

    pendingScrollY.current = window.scrollY;
    setPage(nextPage);
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
    appliedFormat.length > 0 && isCatalogProductFormat(appliedFormat[0])
      ? appliedFormat[0]
      : "all";

  const handleRemoveCategory = (slug: string, source: "applied" | "route") => {
    if (source === "applied") {
      removeAppliedCategorySlug(slug);
      return;
    }

    router.push("/catalog");
  };

  const handleRemoveFormat = (format: string) => {
    removeAppliedFormat(format);

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
            pageSize={pageSize}
            onOpenFilters={handleOpenFilters}
            onToggleSort={() => setSortModalOpen((open) => !open)}
            onSelectSort={handleSortSelect}
            onPageSizeChange={setPageSize}
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

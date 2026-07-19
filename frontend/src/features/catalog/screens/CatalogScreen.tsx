"use client";

import { useRouter } from "next/navigation";
import { useCallback, useEffect, useLayoutEffect, useRef, useState } from "react";
import { useCatalogAuthors } from "@/features/catalog/hooks/useCatalogAuthors";
import { useCatalogCategories } from "@/features/catalog/hooks/useCatalogCategories";
import { useCatalogFilters } from "@/features/catalog/hooks/useCatalogFilters";
import { useCatalogProducts } from "@/features/catalog/hooks/useCatalogProducts";
import { isCatalogProductFormat } from "@/features/catalog/lib/catalog-filter-options";
import {
  buildCatalogUrl,
  parseCatalogQuery,
  replaceCatalogUrlShallow,
  type CatalogQueryRecord,
} from "@/features/catalog/lib/catalog-url-params";
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
  initialQuery?: CatalogQueryRecord;
}

export function CatalogScreen({ categorySlug, initialQuery = {} }: CatalogScreenProps) {
  const router = useRouter();
  // Only category path + format come from the header catalog menu.
  // Do not key off other query fields — shallow sidebar URL updates must not remount/hydrate.
  const headerNavigationKey = `${categorySlug ?? ""}::${parseCatalogQuery(initialQuery).format.join(",")}`;
  const [urlParams] = useState(() => parseCatalogQuery(initialQuery));
  const [seenHeaderNavigationKey, setSeenHeaderNavigationKey] = useState(headerNavigationKey);

  const {
    categories,
    rootCategories,
    selectedRootSlug,
    selectedSubcategorySlug,
    categoryError,
  } = useCatalogCategories(categorySlug);
  const [selectedSort, setSelectedSort] = useState<CatalogProductSort>(
    urlParams.sort ?? DEFAULT_CATALOG_PRODUCT_SORT,
  );
  const [sortModalOpen, setSortModalOpen] = useState(false);
  const [searchInput, setSearchInput] = useState(urlParams.q);
  const [searchQuery, setSearchQuery] = useState(urlParams.q);
  const [page, setPage] = useState(urlParams.page);
  const [filterUpdatePending, setFilterUpdatePending] = useState(false);
  const pendingScrollY = useRef<number | null>(null);
  const [pageSize, setPageSize] = useState(urlParams.pageSize);
  const urlSyncReadyRef = useRef(false);
  const handleProductsLoadComplete = useCallback(() => {
    setFilterUpdatePending(false);
  }, [setFilterUpdatePending]);

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
    hydrateAppliedFilters,
  } = filters;

  // Sync format when header catalog menu navigates. Ignore echoes of our own shallow URL writes
  // by only reacting when the header key changes; keep the route category selected.
  if (seenHeaderNavigationKey !== headerNavigationKey) {
    const parsed = parseCatalogQuery(initialQuery);
    setSeenHeaderNavigationKey(headerNavigationKey);
    hydrateAppliedFilters({
      authors: parsed.authors,
      categorySlugs:
        parsed.categories.length > 0
          ? parsed.categories
          : categorySlug
            ? [categorySlug]
            : [],
      format: parsed.format,
      minPrice: parsed.minPrice,
      maxPrice: parsed.maxPrice,
    });
    setSelectedSort(parsed.sort ?? DEFAULT_CATALOG_PRODUCT_SORT);
    setSearchInput(parsed.q);
    setSearchQuery(parsed.q);
    setPage(parsed.page);
    setPageSize(parsed.pageSize);
    setFilterUpdatePending(true);
  }

  const routeCategorySlugs = getRouteCategorySlugs(selectedRootSlug, selectedSubcategorySlug);

  const { authors, authorsLoading } = useCatalogAuthors({
    categories,
    selectedRootSlug,
    selectedSubcategorySlug,
    appliedCategorySlugs,
  });

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
    page,
    pageSize,
    onLoadComplete: handleProductsLoadComplete,
  });

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      const nextQuery = searchInput.trim();
      let shouldResetPage = false;

      setSearchQuery((current) => {
        if (current === nextQuery) {
          return current;
        }

        shouldResetPage = true;
        return nextQuery;
      });

      if (shouldResetPage) {
        setPage(1);
      }
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
    urlSyncReadyRef.current = true;
  }, []);

  useEffect(() => {
    if (!urlSyncReadyRef.current) return;

    replaceCatalogUrlShallow(
      buildCatalogUrl(categorySlug, {
        authors: appliedAuthors,
        categories: appliedCategorySlugs,
        format: appliedFormat,
        minPrice: appliedMinPrice,
        maxPrice: appliedMaxPrice,
        page,
        searchQuery,
        selectedSort,
        pageSize,
      }),
    );
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
  ]);

  const sortButtonLabel = getCatalogProductSortLabel(selectedSort);
  const filterControlsDisabled = filterUpdatePending || productsLoading;

  const runFilterUpdate = useCallback(
    (update: () => void) => {
      if (filterControlsDisabled) {
        return;
      }

      setFilterUpdatePending(true);
      setPage(1);
      update();
    },
    [filterControlsDisabled, setFilterUpdatePending, setPage],
  );

  const handlePageChange = (nextPage: number) => {
    if (nextPage === currentPage) {
      return;
    }

    pendingScrollY.current = window.scrollY;
    setPage(nextPage);
  };

  const handleSortSelect = (sort: CatalogProductSort) => {
    setSelectedSort(sort);
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
          routeCategorySlugs={routeCategorySlugs}
          appliedCategorySlugs={appliedCategorySlugs}
          appliedAuthors={appliedAuthors}
          appliedFormat={appliedFormat}
          appliedMinPrice={appliedMinPrice}
          appliedMaxPrice={appliedMaxPrice}
          disabled={filterControlsDisabled}
          onToggleRootCategory={(slug) =>
            runFilterUpdate(() => toggleAppliedRootCategory(slug))
          }
          onToggleSubcategory={(rootSlug, subcategorySlug) =>
            runFilterUpdate(() => toggleAppliedSubcategory(rootSlug, subcategorySlug))
          }
          onToggleAuthor={(author) => runFilterUpdate(() => toggleAppliedAuthor(author))}
          onToggleFormat={(format) =>
            runFilterUpdate(() => toggleAppliedFormatValue(format))
          }
          onApplyPriceRange={(minPrice, maxPrice) =>
            runFilterUpdate(() => applyAppliedPriceRange(minPrice, maxPrice))
          }
          onReset={() => runFilterUpdate(resetAppliedFilters)}
        />

        <div className={styles.catalogMain}>
          <CatalogSelectedFilters
            loading={isInitialLoading}
            categories={categories}
            authorOptions={authors}
            routeCategorySlugs={routeCategorySlugs}
            appliedCategorySlugs={appliedCategorySlugs}
            appliedAuthors={appliedAuthors}
            appliedFormat={appliedFormat}
            onRemoveCategory={(slug, source) =>
              runFilterUpdate(() => handleRemoveCategory(slug, source))
            }
            onRemoveAuthor={(author) => runFilterUpdate(() => removeAppliedAuthor(author))}
            onRemoveFormat={(format) => runFilterUpdate(() => handleRemoveFormat(format))}
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
            onApply={() => runFilterUpdate(applyFilters)}
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
            refreshing={filterControlsDisabled && !isInitialLoading}
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

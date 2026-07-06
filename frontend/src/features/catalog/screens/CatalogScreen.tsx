"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import {
  getCatalogCategories,
  getCatalogProducts,
  searchCatalogProducts,
} from "@/features/storefront/api/catalog.api";
import {
  getCategoryFilterIds,
  getChildCategories,
  getRootCategories,
  resolveCategorySelection,
} from "@/features/storefront/lib/catalog-category-filter";
import {
  CATALOG_PRODUCT_SORT_OPTIONS,
  DEFAULT_CATALOG_PRODUCT_SORT,
  getCatalogProductSortLabel,
  sortCatalogProducts,
  type CatalogProductSort,
} from "@/features/storefront/lib/catalog-product-sort";
import type { CatalogCategoryDto, CatalogProductListItemDto } from "@/features/storefront/model/catalog.types";
import { mapCatalogProductToCardData } from "@/features/storefront/lib/map-catalog-product-to-card";
import {
  AUTHOR_FILTER_OPTIONS,
  DEFAULT_CATALOG_MAX_PRICE,
  DEFAULT_CATALOG_MIN_PRICE,
  FORMAT_FILTER_OPTIONS,
  GENRE_FILTER_OPTIONS,
  resolveAppliedPriceFilter,
} from "@/features/catalog/lib/catalog-filter-options";
import { StateBlock } from "@/features/storefront/ui/StateBlock";
import {
  ArrowsSortIcon,
  Button,
  CatalogMenu,
  Checkbox,
  CloseIcon,
  FilterIcon,
  PageLayout,
  Pagination,
  ProductCard,
  ProductCardSkeleton,
} from "@/shared/ui";
import iconStyles from "@/shared/ui/icons/Icon.module.css";
import styles from "./CatalogScreen.module.css";

interface CatalogScreenProps {
  categorySlug?: string;
}

const PAGE_SIZE = 8;

const toggleSingleFilter = (currentValue: string | null, nextValue: string): string | null =>
  currentValue === nextValue ? null : nextValue;

const parsePriceFilter = (value: string): number | undefined => {
  const normalized = value.trim().replace(",", ".");

  if (!normalized) {
    return undefined;
  }

  const parsed = Number(normalized);
  return Number.isFinite(parsed) ? parsed : undefined;
};

export function CatalogScreen({ categorySlug }: CatalogScreenProps) {
  const router = useRouter();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [categories, setCategories] = useState<CatalogCategoryDto[]>([]);
  const [products, setProducts] = useState<CatalogProductListItemDto[]>([]);

  const [filtersOpen, setFiltersOpen] = useState(false);
  const [appliedGenre, setAppliedGenre] = useState<string | null>(null);
  const [appliedAuthor, setAppliedAuthor] = useState<string | null>(null);
  const [appliedFormat, setAppliedFormat] = useState<string | null>(null);
  const [appliedMinPrice, setAppliedMinPrice] = useState("");
  const [appliedMaxPrice, setAppliedMaxPrice] = useState("");
  const [draftGenre, setDraftGenre] = useState<string | null>(null);
  const [draftAuthor, setDraftAuthor] = useState<string | null>(null);
  const [draftFormat, setDraftFormat] = useState<string | null>(null);
  const [draftMinPrice, setDraftMinPrice] = useState(DEFAULT_CATALOG_MIN_PRICE);
  const [draftMaxPrice, setDraftMaxPrice] = useState(DEFAULT_CATALOG_MAX_PRICE);
  const [selectedRootSlug, setSelectedRootSlug] = useState<string | null>(null);
  const [selectedSubcategorySlug, setSelectedSubcategorySlug] = useState<string | null>(null);
  const [selectedSort, setSelectedSort] = useState<CatalogProductSort>(DEFAULT_CATALOG_PRODUCT_SORT);
  const [sortModalOpen, setSortModalOpen] = useState(false);
  const [catalogOpen, setCatalogOpen] = useState(false);
  const [searchInput, setSearchInput] = useState("");
  const [searchQuery, setSearchQuery] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const pendingScrollY = useRef<number | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        setError(null);
        const categoriesData = await getCatalogCategories();

        setCategories(categoriesData.filter((category) => category.isActive));
      } catch {
        setError("Не вдалося завантажити категорії");
      }
    };

    void load();
  }, []);

  useEffect(() => {
    if (categories.length === 0) {
      return;
    }

    if (!categorySlug) {
      setSelectedRootSlug(null);
      setSelectedSubcategorySlug(null);
      return;
    }

    const selection = resolveCategorySelection(categories, categorySlug);
    setSelectedRootSlug(selection.rootSlug);
    setSelectedSubcategorySlug(selection.subcategorySlug);
  }, [categorySlug, categories]);

  useEffect(() => {
    setCurrentPage(1);
  }, [
    appliedAuthor,
    appliedFormat,
    appliedGenre,
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

  useEffect(() => {
    let cancelled = false;

    const loadProducts = async () => {
      try {
        setLoading(true);
        setError(null);

        const selectedCategory = selectedSubcategorySlug
          ? categories.find((category) => category.slug === selectedSubcategorySlug)
          : selectedRootSlug
            ? categories.find((category) => category.slug === selectedRootSlug)
            : null;
        const categoryFilterIds = selectedCategory
          ? getCategoryFilterIds(categories, selectedCategory)
          : undefined;
        const minPrice = parsePriceFilter(appliedMinPrice);
        const maxPrice = parsePriceFilter(appliedMaxPrice);
        const shouldUseSearchEndpoint = Boolean(
          searchQuery ||
            selectedCategory ||
            appliedAuthor ||
            appliedFormat ||
            appliedGenre ||
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
                author: appliedAuthor ?? undefined,
                format: appliedFormat ?? undefined,
                genre: appliedGenre ?? undefined,
                sort: selectedSort !== DEFAULT_CATALOG_PRODUCT_SORT ? selectedSort : undefined,
                page: 1,
                pageSize: 200,
              })
            ).items
          : await getCatalogProducts();

        if (!cancelled) {
          setProducts(nextProducts);
        }
      } catch {
        if (!cancelled) {
          setError("Не вдалося завантажити товари");
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    };

    void loadProducts();

    return () => {
      cancelled = true;
    };
  }, [
    appliedAuthor,
    appliedFormat,
    appliedGenre,
    appliedMaxPrice,
    appliedMinPrice,
    categories,
    searchQuery,
    selectedRootSlug,
    selectedSubcategorySlug,
    selectedSort,
  ]);

  const rootCategories = useMemo(() => getRootCategories(categories), [categories]);

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

  const filteredProducts = useMemo(() => {
    return sortCatalogProducts(products, selectedSort);
  }, [products, selectedSort]);

  const isInitialLoading = loading && products.length === 0;

  const totalPages = Math.max(1, Math.ceil(filteredProducts.length / PAGE_SIZE));

  useEffect(() => {
    if (currentPage > totalPages) {
      setCurrentPage(totalPages);
    }
  }, [currentPage, totalPages]);

  const paginatedProducts = filteredProducts.slice(
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
    setCurrentPage(page);
  };

  const handleSortSelect = (sort: CatalogProductSort) => {
    setSelectedSort(sort);
    setSortModalOpen(false);
  };

  const handleOpenFilters = () => {
    setSortModalOpen(false);
    setDraftGenre(appliedGenre);
    setDraftAuthor(appliedAuthor);
    setDraftFormat(appliedFormat);
    setDraftMinPrice(appliedMinPrice || DEFAULT_CATALOG_MIN_PRICE);
    setDraftMaxPrice(appliedMaxPrice || DEFAULT_CATALOG_MAX_PRICE);
    setFiltersOpen(true);
  };

  const handleApplyFilters = () => {
    setAppliedGenre(draftGenre);
    setAppliedAuthor(draftAuthor);
    setAppliedFormat(draftFormat);
    setAppliedMinPrice(resolveAppliedPriceFilter(draftMinPrice, DEFAULT_CATALOG_MIN_PRICE));
    setAppliedMaxPrice(resolveAppliedPriceFilter(draftMaxPrice, DEFAULT_CATALOG_MAX_PRICE));
    setFiltersOpen(false);
  };

  const handleRootCategoryClick = (slug: string) => {
    if (selectedRootSlug === slug) {
      router.push("/catalog");
      return;
    }

    router.push(`/catalog/${slug}`);
  };

  const handleSubcategoryClick = (slug: string) => {
    if (selectedSubcategorySlug === slug) {
      router.push(selectedRootSlug ? `/catalog/${selectedRootSlug}` : "/catalog");
      return;
    }

    router.push(`/catalog/${slug}`);
  };

  const handleCatalogCategorySelect = (slug: string) => {
    router.push(`/catalog/${slug}`);
  };

  const handleShowAllCategories = () => {
    router.push("/catalog");
  };

  return (
    <PageLayout
      headerProps={{
        homeHref: "/home",
        userHref: "/me",
        searchPlaceholder: "Пошук книг",
        onSearchQueryChange: setSearchInput,
        onMenuClick: () => setCatalogOpen(true),
      }}
      footerProps={{ homeHref: "/home" }}
    >
      <h1 className={styles.pageTitle}>Каталог</h1>

      {isInitialLoading ? (
        <div className={styles.skeletonRow} aria-hidden="true">
          {Array.from({ length: 4 }, (_, index) => (
            <div key={index} className={styles.skeletonChip} />
          ))}
        </div>
      ) : rootCategories.length > 0 ? (
        <div className={styles.categoryRows}>
          <div className={styles.categories} role="tablist" aria-label="Основні категорії">
            {rootCategories.map((category) => {
              const isActive = selectedRootSlug === category.slug;

              return (
                <Button
                  key={category.id}
                  type="button"
                  role="tab"
                  variant="dark"
                  size="sm"
                  selectable
                  selected={isActive}
                  aria-selected={isActive}
                  onClick={() => handleRootCategoryClick(category.slug)}
                >
                  {category.name}
                </Button>
              );
            })}
          </div>

          {visibleSubcategories.length > 0 ? (
            <div className={styles.subcategories} role="tablist" aria-label="Підкатегорії">
              {visibleSubcategories.map((category) => {
                const isActive = selectedSubcategorySlug === category.slug;

                return (
                  <Button
                    key={category.id}
                    type="button"
                    role="tab"
                    variant="dark"
                    size="sm"
                    selectable
                    selected={isActive}
                    aria-selected={isActive}
                    onClick={() => handleSubcategoryClick(category.slug)}
                  >
                    {category.name}
                  </Button>
                );
              })}
            </div>
          ) : null}
        </div>
      ) : null}

      <div className={styles.toolbar}>
        {isInitialLoading ? (
          <div className={styles.skeletonToolbar} aria-hidden="true">
            <div className={styles.skeletonButton} />
            <div className={styles.skeletonButton} />
          </div>
        ) : (
          <>
            <Button
              type="button"
              variant="dark"
              size="lg"
              fullWidth
              leadingIcon={<FilterIcon className={iconStyles.icon} />}
              aria-haspopup="dialog"
              aria-expanded={filtersOpen}
              onClick={handleOpenFilters}
            >
              Фільтри
            </Button>

            <Button
              type="button"
              variant="dark"
              size="lg"
              fullWidth
              leadingIcon={<ArrowsSortIcon className={iconStyles.icon} />}
              aria-haspopup="dialog"
              aria-expanded={sortModalOpen}
              onClick={() => setSortModalOpen((open) => !open)}
            >
              {sortButtonLabel}
            </Button>
          </>
        )}
      </div>

      {filtersOpen && !isInitialLoading ? (
        <section className={styles.filterPanel} role="dialog" aria-modal="true" aria-label="Фільтри">
          <div className={styles.filterHeader}>
            <h2 className={styles.filterTitle}>Фільтри</h2>
            <button
              type="button"
              className={styles.filterCloseButton}
              aria-label="Закрити фільтри"
              onClick={() => setFiltersOpen(false)}
            >
              <CloseIcon className={iconStyles.icon} />
            </button>
          </div>

          <div className={styles.filterContent}>
            <section className={styles.filterSection} aria-labelledby="genre-filter-title">
              <h3 id="genre-filter-title" className={styles.filterSectionTitle}>
                Жанр
              </h3>
              <div className={styles.filterList}>
                {GENRE_FILTER_OPTIONS.map((option) => (
                  <Checkbox
                    key={option.value}
                    label={option.label}
                    checked={draftGenre === option.value}
                    onCheckedChange={() =>
                      setDraftGenre((current) => toggleSingleFilter(current, option.value))
                    }
                  />
                ))}
              </div>
              <span className={styles.showAll}>Показати всі</span>
            </section>

            <section
              className={[styles.filterSection, styles.authorSection].join(" ")}
              aria-labelledby="author-filter-title"
            >
              <div>
                <h3 id="author-filter-title" className={styles.filterSectionTitle}>
                  Автор
                </h3>
                <div className={styles.filterList}>
                  {AUTHOR_FILTER_OPTIONS.map((option) => (
                    <Checkbox
                      key={option.value}
                      label={option.label}
                      checked={draftAuthor === option.value}
                      onCheckedChange={() =>
                        setDraftAuthor((current) => toggleSingleFilter(current, option.value))
                      }
                    />
                  ))}
                </div>
                <span className={styles.showAll}>Показати всі</span>
              </div>
              <img className={styles.filterCat} src="/filter-cat.svg" alt="" aria-hidden="true" />
            </section>

            <section className={styles.filterSection} aria-labelledby="format-filter-title">
              <h3 id="format-filter-title" className={styles.filterSectionTitle}>
                Формат книги
              </h3>
              <div className={styles.filterList}>
                {FORMAT_FILTER_OPTIONS.map((option) => (
                  <Checkbox
                    key={option.value}
                    label={option.label}
                    checked={draftFormat === option.value}
                    onCheckedChange={() =>
                      setDraftFormat((current) => toggleSingleFilter(current, option.value))
                    }
                  />
                ))}
              </div>
            </section>

            <section className={styles.filterSection} aria-labelledby="price-filter-title">
              <h3 id="price-filter-title" className={styles.filterSectionTitle}>
                Ціна
              </h3>
              <div className={styles.priceRow}>
                <label className={styles.priceField}>
                  <span className={styles.priceLabel}>Від</span>
                  <input
                    className={styles.priceInput}
                    inputMode="numeric"
                    value={draftMinPrice}
                    onChange={(event) => setDraftMinPrice(event.target.value)}
                  />
                </label>
                <label className={styles.priceField}>
                  <span className={styles.priceLabel}>До</span>
                  <input
                    className={styles.priceInput}
                    inputMode="numeric"
                    value={draftMaxPrice}
                    onChange={(event) => setDraftMaxPrice(event.target.value)}
                  />
                </label>
              </div>
            </section>
          </div>

          <div className={styles.filterActions}>
            <Button
              type="button"
              variant="primary"
              size="lg"
              fullWidth
              className={styles.applyFiltersButton}
              onClick={handleApplyFilters}
            >
              Застосувати
            </Button>
          </div>
        </section>
      ) : null}

      {error ? <StateBlock message={error} isError /> : null}

      {isInitialLoading ? (
        <div className={styles.skeletonGrid} aria-hidden="true">
          {Array.from({ length: 8 }, (_, index) => (
            <ProductCardSkeleton key={index} />
          ))}
        </div>
      ) : !loading && !error && filteredProducts.length === 0 ? (
        <p className={styles.emptyState} role="status">
          Товарів за обраними фільтрами не знайдено
        </p>
      ) : null}

      {!loading && !error && paginatedProducts.length > 0 ? (
        <>
          <div className={styles.productGrid}>
            {paginatedProducts.map((product) => (
              <Link
                key={product.id}
                href={`/products/${product.slug}`}
                className={styles.cardLink}
              >
                <ProductCard product={mapCatalogProductToCardData(product)} />
              </Link>
            ))}
          </div>

          {totalPages > 1 ? (
            <div className={styles.paginationWrap}>
              <Pagination
                currentPage={currentPage}
                totalPages={totalPages}
                onPageChange={handlePageChange}
              />
            </div>
          ) : null}
        </>
      ) : null}

      {sortModalOpen ? (
        <>
          <button
            type="button"
            className={styles.sortBackdrop}
            aria-label="Закрити сортування"
            onClick={() => setSortModalOpen(false)}
          />
          <div className={styles.sortSheet} role="dialog" aria-modal="true" aria-label="Сортування">
            <div className={styles.sortSheetOptions}>
              {CATALOG_PRODUCT_SORT_OPTIONS.map((option) => {
                const isActive = selectedSort === option.value;

                return (
                  <button
                    key={option.value}
                    type="button"
                    className={isActive ? styles.sortOptionActive : styles.sortOption}
                    aria-pressed={isActive}
                    onClick={() => handleSortSelect(option.value)}
                  >
                    {option.label}
                  </button>
                );
              })}
            </div>
          </div>
        </>
      ) : null}

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
      />
    </PageLayout>
  );
}

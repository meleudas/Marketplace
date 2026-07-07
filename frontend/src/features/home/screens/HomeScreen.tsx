"use client";

import Link from "next/link";
import { useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import {
  getCatalogCategories,
  getCatalogNewProducts,
  getCatalogOnSaleProducts,
  getCatalogPopularProducts,
  getPersonalizedRecommendations,
} from "@/features/storefront/api/catalog.api";
import {
  CATALOG_PRODUCT_SORT_OPTIONS,
  getCatalogProductSortLabel,
  sortCatalogProducts,
  type CatalogProductSort,
} from "@/features/storefront/lib/catalog-product-sort";
import type { CatalogCategoryDto, CatalogProductListItemDto } from "@/features/storefront/model/catalog.types";
import { StateBlock } from "@/features/storefront/ui/StateBlock";
import {
  ArrowsSortIcon,
  Button,
  Checkbox,
  CloseIcon,
  FilterIcon,
  PageLayout,
  Pagination,
  ProductCard,
  Radio,
  RadioGroup,
} from "@/shared/ui";
import iconStyles from "@/shared/ui/icons/Icon.module.css";
import type { ProductCardData } from "@/shared/ui/ProductCard";
import type { CatalogCategoryDto } from "@/features/storefront/model/catalog.types";
import { mapProductToRailCard, type ProductRailCard } from "@/features/home/lib/map-product-to-rail-card";
import { CatalogMenu, PageLayout } from "@/shared/ui";
import { ProductRailItems } from "../ui/ProductRailItems";
import { RecommendationsRail } from "../ui/RecommendationsRail";
import styles from "./HomeScreen.module.css";

const HOME_RAIL_PAGE_SIZE = 12;

export function HomeScreen() {
  const router = useRouter();
  const [catalogOpen, setCatalogOpen] = useState(false);
  const [categories, setCategories] = useState<CatalogCategoryDto[]>([]);
  const [recommendations, setRecommendations] = useState<ProductRailCard[]>([]);
  const [popular, setPopular] = useState<ProductRailCard[]>([]);
  const [newProducts, setNewProducts] = useState<ProductRailCard[]>([]);
  const [onSale, setOnSale] = useState<ProductRailCard[]>([]);
  const [recommendationsLoading, setRecommendationsLoading] = useState(true);
  const [popularLoading, setPopularLoading] = useState(true);
  const [newProductsLoading, setNewProductsLoading] = useState(true);
  const [onSaleLoading, setOnSaleLoading] = useState(true);

  const [feedItems, setFeedItems] = useState<ProductRailCard[]>([]);
  const [feedLoading, setFeedLoading] = useState(false);
  const [feedPage, setFeedPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);
  const [selectedCategory, setSelectedCategory] = useState<number | null>(null);

  const loadFeed = useCallback(async (page: number, categoryId: number | null, append: boolean) => {
    setFeedLoading(true);
    try {
      const response = await searchCatalogProducts({
        categoryIds: categoryId ? [categoryId] : undefined,
        page,
        pageSize: 12,
      });
      const newItems = response.items.map(mapProductToRailCard);
      
      setFeedItems((prev) => (append ? [...prev, ...newItems] : newItems));
      setHasMore(response.items.length === 12);
    } catch {
    } finally {
      setFeedLoading(false);
    }
  }, []);

  useEffect(() => {
    const load = async () => {
      try {
        const categoriesData = await getCatalogCategories();
        setCategories(categoriesData.filter((category) => category.isActive));
      } catch {
        setCategories([]);
      }
    };

    void load();
  }, []);

  useEffect(() => {
    setCurrentPage(1);
  }, [selectedCategorySlug, inStockOnly, selectedSort, searchQuery]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setSearchQuery(searchInput.trim());
    }, 350);

    return () => window.clearTimeout(timeoutId);
  }, [searchInput]);

  useEffect(() => {
    let cancelled = false;

    const loadRecommendations = async () => {
      try {
        setRecommendationsLoading(true);
        const result = await getPersonalizedRecommendations({ limit: HOME_RAIL_PAGE_SIZE });

        if (!cancelled) {
          setRecommendations(result.items.map(mapProductToRailCard));
        }
      } catch {
        if (!cancelled) {
          setRecommendations([]);
        }
      }
      finally {
        if (!cancelled) {
          setRecommendationsLoading(false);
        }
      }
    };

    void loadRecommendations();

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    let cancelled = false;

    const loadBrowseRails = async () => {
      setPopularLoading(true);
      setNewProductsLoading(true);
      setOnSaleLoading(true);

      const [popularResult, newResult, onSaleResult] = await Promise.allSettled([
        getCatalogPopularProducts({ pageSize: HOME_RAIL_PAGE_SIZE }),
        getCatalogNewProducts({ pageSize: HOME_RAIL_PAGE_SIZE }),
        getCatalogOnSaleProducts({ pageSize: HOME_RAIL_PAGE_SIZE }),
      ]);

      if (cancelled) {
        return;
      }

      setPopular(
        popularResult.status === "fulfilled"
          ? popularResult.value.items.map(mapProductToRailCard)
          : [],
      );
      setNewProducts(
        newResult.status === "fulfilled" ? newResult.value.items.map(mapProductToRailCard) : [],
      );
      setOnSale(
        onSaleResult.status === "fulfilled" ? onSaleResult.value.items.map(mapProductToRailCard) : [],
      );
      setPopularLoading(false);
      setNewProductsLoading(false);
      setOnSaleLoading(false);
    };

    void loadBrowseRails();

    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <PageLayout
      headerProps={{
        homeHref: "/",
        userHref: "/me",
        onMenuClick: () => setCatalogOpen(true),
      }}
      footerProps={{ homeHref: "/" }}
    >
      <section className={styles.promoBanner} aria-label="Рекламний банер">
        <div className={styles.promoContent}>
          <p className={styles.promoEyebrow}>Не пропусти!</p>
          <p className={styles.promoQuote}>“Літературне чаювання”</p>
          <p className={styles.promoDate}>
            11:00 - Середа
            <br />
            15 квітня 2026
          </p>
        </div>
      ) : null}

      <div className={styles.toolbar}>
        <Button
          type="button"
          variant="dark"
          size="lg"
          fullWidth
          leadingIcon={<FilterIcon className={iconStyles.icon} />}
          aria-expanded={filtersOpen}
          onClick={() => setFiltersOpen((open) => !open)}
        >
          Фільтри
        </Button>

        <div className={styles.sortAnchor}>
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

          {sortModalOpen ? (
            <div
              className={styles.sortPopover}
              role="dialog"
              aria-modal="true"
              aria-labelledby="sort-modal-title"
            >
              <header className={styles.modalHeader}>
                <h2 id="sort-modal-title" className={styles.modalTitle}>
                  Сортувати
                </h2>
                <button
                  type="button"
                  className={styles.modalClose}
                  aria-label="Закрити"
                  onClick={() => setSortModalOpen(false)}
                >
                  <CloseIcon className={iconStyles.icon} />
                </button>
              </header>

              <RadioGroup
                name="product-sort"
                value={selectedSort ?? ""}
                onValueChange={(value) => handleSortSelect(value as CatalogProductSort)}
                className={styles.sortOptions}
              >
                {CATALOG_PRODUCT_SORT_OPTIONS.map((option) => (
                  <Radio key={option.value} value={option.value} label={option.label} />
                ))}
              </RadioGroup>
            </div>
          ) : null}
        </div>
      </div>

      {filtersOpen ? (
        <section className={styles.filterPanel} aria-label="Фільтри">
          <Checkbox
            label="Тільки в наявності"
            checked={inStockOnly}
            onCheckedChange={setInStockOnly}
          />
        </section>
      ) : null}

      {loading ? <StateBlock message="Завантаження..." /> : null}
      {error ? <StateBlock message={error} isError /> : null}

      {!loading && !error && filteredProducts.length === 0 ? (
        <StateBlock message="Товарів за обраними фільтрами не знайдено" />
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
                <ProductCard product={mapProductToCard(product, productImages[product.slug])} />
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
        <button
          type="button"
          className={styles.sortBackdrop}
          aria-label="Закрити сортування"
          onClick={() => setSortModalOpen(false)}
        />
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
        onCategorySelect={(slug, format) => {
          const formatParam = format === "all" ? "" : `?format=${encodeURIComponent(format)}`;
          router.push(`/catalog/${slug}${formatParam}`);
        }}
        onShowAll={(format) => {
          const formatParam = format === "all" ? "" : `?format=${encodeURIComponent(format)}`;
          router.push(`/catalog${formatParam}`);
        }}
      />
    </PageLayout>
  );
}

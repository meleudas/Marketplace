"use client";

<<<<<<< Updated upstream
import Link from "next/link";
import { useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import {
  getCatalogCategories,
  getCatalogProductBySlug,
  getCatalogProducts,
  searchCatalogProducts,
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
=======
import Image from "next/image";
import { useEffect, useState, useMemo, useRef, useCallback } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import {
  getCatalogCategories,
  getCatalogNewProducts,
  getCatalogOnSaleProducts,
  getCatalogPopularProducts,
  getPersonalizedRecommendations,
  searchCatalogProducts,
} from "@/features/storefront/api/catalog.api";
import type { CatalogCategoryDto } from "@/features/storefront/model/catalog.types";
import { mapProductToRailCard, type ProductRailCard } from "@/features/home/lib/map-product-to-rail-card";
import { CatalogMenu, PageLayout, ProductCard, Spinner } from "@/shared/ui";
import { ProductRailItems } from "../ui/ProductRailItems";
import { RecommendationsRail } from "../ui/RecommendationsRail";
>>>>>>> Stashed changes
import styles from "./HomeScreen.module.css";

const PAGE_SIZE = 8;

const mapProductToCard = (
  product: CatalogProductListItemDto,
  imageUrl?: string | null,
): ProductCardData => ({
  id: String(product.id),
  title: product.name,
  price: product.price,
  imageUrl: imageUrl ?? undefined,
  inStock: product.availabilityStatus !== "out_of_stock" && product.availableQty > 0,
});

const filterProductsLocally = (
  items: CatalogProductListItemDto[],
  options: {
    query: string;
    categoryId?: number;
    inStockOnly: boolean;
    sort: CatalogProductSort | null;
  },
): CatalogProductListItemDto[] => {
  const query = options.query.toLowerCase();
  let result = [...items];

  if (options.categoryId) {
    result = result.filter((product) => product.categoryId === options.categoryId);
  }

  if (options.inStockOnly) {
    result = result.filter(
      (product) => product.availabilityStatus !== "out_of_stock" && product.availableQty > 0,
    );
  }

  if (query) {
    result = result.filter((product) =>
      [product.name, product.description, product.slug].some((value) =>
        value.toLowerCase().includes(query),
      ),
    );
  }

  return options.sort ? sortCatalogProducts(result, options.sort) : result;
};

export function HomeScreen() {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [categories, setCategories] = useState<CatalogCategoryDto[]>([]);
  const [products, setProducts] = useState<CatalogProductListItemDto[]>([]);
  const [productImages, setProductImages] = useState<Record<string, string | null>>({});

  const [filtersOpen, setFiltersOpen] = useState(false);
  const [inStockOnly, setInStockOnly] = useState(false);
  const [selectedCategorySlug, setSelectedCategorySlug] = useState<string | null>(null);
  const [selectedSort, setSelectedSort] = useState<CatalogProductSort | null>(null);
  const [sortModalOpen, setSortModalOpen] = useState(false);
  const [searchInput, setSearchInput] = useState("");
  const [searchQuery, setSearchQuery] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const pendingScrollY = useRef<number | null>(null);
  const productImagesRef = useRef<Record<string, string | null>>({});

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
<<<<<<< Updated upstream
    setCurrentPage(1);
  }, [selectedCategorySlug, inStockOnly, selectedSort, searchQuery]);
=======
    setFeedPage(1);
    setHasMore(true);
    void loadFeed(1, selectedCategory, false);
  }, [selectedCategory, loadFeed]);

  const observerRef = useRef<IntersectionObserver | null>(null);
  const triggerRef = useCallback(
    (node: HTMLDivElement | null) => {
      if (feedLoading) return;
      if (observerRef.current) observerRef.current.disconnect();

      observerRef.current = new IntersectionObserver((entries) => {
        if (entries[0].isIntersecting && hasMore) {
          const nextPage = feedPage + 1;
          setFeedPage(nextPage);
          void loadFeed(nextPage, selectedCategory, true);
        }
      });

      if (node) observerRef.current.observe(node);
    },
    [feedLoading, hasMore, feedPage, selectedCategory, loadFeed]
  );

  useEffect(() => {
    let cancelled = false;
>>>>>>> Stashed changes

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

        const selectedCategory = selectedCategorySlug
          ? categories.find((category) => category.slug === selectedCategorySlug)
          : null;
        const shouldUseSearchEndpoint = Boolean(
          searchQuery || selectedCategory || inStockOnly || selectedSort,
        );

        let nextProducts = await getCatalogProducts();

        if (shouldUseSearchEndpoint) {
          const searchResult = await searchCatalogProducts({
            query: searchQuery || undefined,
            categoryIds: selectedCategory ? [selectedCategory.id] : undefined,
            availabilityStatus: inStockOnly ? "in_stock" : undefined,
            sort: selectedSort ?? undefined,
            page: 1,
            pageSize: 200,
          });

          nextProducts =
            searchResult.items.length > 0
              ? searchResult.items
              : filterProductsLocally(nextProducts, {
                  query: searchQuery,
                  categoryId: selectedCategory?.id,
                  inStockOnly,
                  sort: selectedSort,
                });
        }

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
  }, [categories, inStockOnly, searchQuery, selectedCategorySlug, selectedSort]);

  const filteredProducts = useMemo(() => {
    return selectedSort && !searchQuery ? sortCatalogProducts(products, selectedSort) : products;
  }, [products, searchQuery, selectedSort]);

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

  productImagesRef.current = productImages;

  useLayoutEffect(() => {
    if (pendingScrollY.current === null) {
      return;
    }

    window.scrollTo(0, pendingScrollY.current);
    pendingScrollY.current = null;
  }, [currentPage, paginatedProducts]);

  useEffect(() => {
    const slugsToLoad = products
      .map((product) => product.slug)
      .filter((slug) => !(slug in productImagesRef.current));

    if (slugsToLoad.length === 0) {
      return;
    }

    let cancelled = false;

    const loadImages = async () => {
      const entries = await Promise.all(
        slugsToLoad.map(async (slug) => {
          try {
            const details = await getCatalogProductBySlug(slug);
            const image = details.images[0]?.thumbnailUrl ?? details.images[0]?.imageUrl ?? null;
            return [slug, image] as const;
          } catch {
            return [slug, null] as const;
          }
        }),
      );

      if (cancelled) {
        return;
      }

      setProductImages((current) => {
        const next = { ...current };
        for (const [slug, image] of entries) {
          next[slug] = image;
        }
        return next;
      });
    };

    void loadImages();

    return () => {
      cancelled = true;
    };
  }, [products]);

  const sortButtonLabel = selectedSort ? getCatalogProductSortLabel(selectedSort) : "Сортувати";

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

  return (
    <PageLayout
      headerProps={{
        homeHref: "/",
        userHref: "/me",
        searchValue: searchInput,
        searchPlaceholder: "Пошук книг",
        onSearchChange: setSearchInput,
      }}
      footerProps={{ homeHref: "/" }}
    >
      {categories.length > 0 ? (
        <div className={styles.categories} role="tablist" aria-label="Категорії">
          {categories.map((category) => {
            const isActive = selectedCategorySlug === category.slug;

            return (
              <button
                key={category.id}
                type="button"
                role="tab"
                aria-selected={isActive}
                className={`${styles.categoryChip} ${isActive ? styles.categoryChipActive : ""}`.trim()}
                onClick={() =>
                  setSelectedCategorySlug((current) => (current === category.slug ? null : category.slug))
                }
              >
                {category.name}
              </button>
            );
          })}
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

<<<<<<< Updated upstream
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
=======
      <RecommendationsRail title="Рекомендовані" loading={recommendationsLoading} viewAllHref="/catalog">
        <ProductRailItems items={recommendations} />
      </RecommendationsRail>

      <RecommendationsRail title="Популярні" loading={popularLoading} viewAllHref="/catalog?sort=relevance">
        <ProductRailItems items={popular} />
      </RecommendationsRail>

      <RecommendationsRail title="Новинки" loading={newProductsLoading} viewAllHref="/catalog?sort=newest">
        <ProductRailItems items={newProducts} />
      </RecommendationsRail>

      <RecommendationsRail title="Акції" loading={onSaleLoading} viewAllHref="/catalog">
        <ProductRailItems items={onSale} />
      </RecommendationsRail>

      <section className={styles.feedSection}>
        <div className={styles.feedHeader}>
          <h2 className={styles.feedTitle}>Спеціально для вас</h2>
          
          <div className={styles.tagPillsList} role="tablist">
            <button
              type="button"
              role="tab"
              aria-selected={selectedCategory === null}
              className={`${styles.tagPill} ${selectedCategory === null ? styles.tagPillActive : ""}`}
              onClick={() => setSelectedCategory(null)}
            >
              Всі
            </button>
            {categories
              .filter((c) => c.parentId === null && c.productCount > 0)
              .map((cat) => (
                <button
                  type="button"
                  key={cat.id}
                  role="tab"
                  aria-selected={selectedCategory === cat.id}
                  className={`${styles.tagPill} ${selectedCategory === cat.id ? styles.tagPillActive : ""}`}
                  onClick={() => setSelectedCategory(cat.id)}
                >
                  #{cat.name}
                </button>
              ))}
          </div>
        </div>

        {feedItems.length > 0 ? (
          <div className={styles.feedGrid}>
            {feedItems.map((item) => (
              <Link key={item.id} href={item.href} className={styles.feedCardLink}>
                <ProductCard product={item} />
              </Link>
            ))}
          </div>
        ) : (
          !feedLoading && (
            <div className={styles.feedEmpty}>
              Немає товарів у цій категорії
            </div>
          )
        )}

        {feedLoading && (
          <div className={styles.feedLoading}>
            <Spinner />
          </div>
        )}

        {hasMore && <div ref={triggerRef} className={styles.scrollTrigger} />}
      </section>

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
>>>>>>> Stashed changes
    </PageLayout>
  );
}

"use client";

import Link from "next/link";
import { useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import {
  getCatalogCategories,
  getCatalogProductBySlug,
  getCatalogProducts,
  searchCatalogProducts,
} from "@/features/storefront/api/catalog.api";
import {
  getCategoryFilterIds,
  getChildCategories,
  getRootCategories,
  productMatchesCategoryFilter,
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
import { StateBlock } from "@/features/storefront/ui/StateBlock";
import {
  ArrowsSortIcon,
  Button,
  CatalogMenu,
  Checkbox,
  FilterIcon,
  PageLayout,
  Pagination,
  ProductCard,
} from "@/shared/ui";
import iconStyles from "@/shared/ui/icons/Icon.module.css";
import type { ProductCardData } from "@/shared/ui/ProductCard";
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
    categoryFilterIds?: number[];
    inStockOnly: boolean;
    sort: CatalogProductSort | null;
  },
): CatalogProductListItemDto[] => {
  const query = options.query.toLowerCase();
  let result = [...items];

  if (options.categoryFilterIds && options.categoryFilterIds.length > 0) {
    result = result.filter((product) =>
      productMatchesCategoryFilter(product.categoryId, options.categoryFilterIds!),
    );
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
  const [selectedRootSlug, setSelectedRootSlug] = useState<string | null>(null);
  const [selectedSubcategorySlug, setSelectedSubcategorySlug] = useState<string | null>(null);
  const [selectedSort, setSelectedSort] = useState<CatalogProductSort>(DEFAULT_CATALOG_PRODUCT_SORT);
  const [sortModalOpen, setSortModalOpen] = useState(false);
  const [catalogOpen, setCatalogOpen] = useState(false);
  const [searchInput, setSearchInput] = useState("");
  const [searchQuery, setSearchQuery] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const pendingScrollY = useRef<number | null>(null);
  const productImagesRef = useRef<Record<string, string | null>>({});

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
    setCurrentPage(1);
  }, [selectedRootSlug, selectedSubcategorySlug, inStockOnly, selectedSort, searchQuery]);

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
        const shouldUseSearchEndpoint = Boolean(
          searchQuery || selectedCategory || inStockOnly || selectedSort !== DEFAULT_CATALOG_PRODUCT_SORT,
        );

        let nextProducts = await getCatalogProducts();

        if (shouldUseSearchEndpoint) {
          const searchResult = await searchCatalogProducts({
            query: searchQuery || undefined,
            categoryIds: categoryFilterIds,
            availabilityStatus: inStockOnly ? "in_stock" : undefined,
            sort: selectedSort !== DEFAULT_CATALOG_PRODUCT_SORT ? selectedSort : undefined,
            page: 1,
            pageSize: 200,
          });

          nextProducts =
            searchResult.items.length > 0
              ? searchResult.items
              : filterProductsLocally(nextProducts, {
                  query: searchQuery,
                  categoryFilterIds,
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
  }, [categories, inStockOnly, searchQuery, selectedRootSlug, selectedSubcategorySlug, selectedSort]);

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

  useEffect(() => {
    if (!sortModalOpen) {
      return;
    }

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    return () => {
      document.body.style.overflow = previousOverflow;
    };
  }, [sortModalOpen]);

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

  const handleRootCategoryClick = (slug: string) => {
    setSelectedRootSlug((current) => {
      if (current === slug) {
        setSelectedSubcategorySlug(null);
        return null;
      }

      setSelectedSubcategorySlug(null);
      return slug;
    });
  };

  const handleSubcategoryClick = (slug: string) => {
    setSelectedSubcategorySlug((current) => (current === slug ? null : slug));
  };

  const handleCatalogCategorySelect = (slug: string) => {
    const selection = resolveCategorySelection(categories, slug);
    setSelectedRootSlug(selection.rootSlug);
    setSelectedSubcategorySlug(selection.subcategorySlug);
  };

  const handleShowAllCategories = () => {
    setSelectedRootSlug(null);
    setSelectedSubcategorySlug(null);
  };

  return (
    <PageLayout
      headerProps={{
        homeHref: "/home",
        userHref: "/me",
        searchValue: searchInput,
        searchPlaceholder: "Пошук книг",
        onSearchChange: setSearchInput,
        onMenuClick: () => setCatalogOpen(true),
      }}
      footerProps={{ homeHref: "/home" }}
    >
      {rootCategories.length > 0 ? (
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

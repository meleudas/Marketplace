"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import {
  getCatalogCategories,
  getCatalogProductBySlug,
  getCatalogProducts,
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
  const [currentPage, setCurrentPage] = useState(1);

  useEffect(() => {
    const load = async () => {
      try {
        setLoading(true);
        setError(null);

        const [categoriesData, productsData] = await Promise.all([
          getCatalogCategories(),
          getCatalogProducts(),
        ]);

        setCategories(categoriesData.filter((category) => category.isActive));
        setProducts(productsData);
      } catch {
        setError("Не вдалося завантажити товари");
      } finally {
        setLoading(false);
      }
    };

    void load();
  }, []);

  useEffect(() => {
    setCurrentPage(1);
  }, [selectedCategorySlug, inStockOnly, selectedSort]);

  const filteredProducts = useMemo(() => {
    let result = [...products];

    if (selectedCategorySlug) {
      const selectedCategory = categories.find((category) => category.slug === selectedCategorySlug);
      if (selectedCategory) {
        result = result.filter((product) => product.categoryId === selectedCategory.id);
      }
    }

    if (inStockOnly) {
      result = result.filter(
        (product) => product.availabilityStatus !== "out_of_stock" && product.availableQty > 0,
      );
    }

    if (selectedSort) {
      result = sortCatalogProducts(result, selectedSort);
    }

    return result;
  }, [categories, inStockOnly, products, selectedCategorySlug, selectedSort]);

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

  useEffect(() => {
    const slugsToLoad = paginatedProducts
      .map((product) => product.slug)
      .filter((slug) => !(slug in productImages));

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
  }, [paginatedProducts, productImages]);

  const sortButtonLabel = selectedSort ? getCatalogProductSortLabel(selectedSort) : "Сортувати";

  const handleSortSelect = (sort: CatalogProductSort) => {
    setSelectedSort(sort);
    setSortModalOpen(false);
  };

  return (
    <PageLayout headerProps={{ homeHref: "/home", userHref: "/me" }} footerProps={{ homeHref: "/home" }}>
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
                onPageChange={setCurrentPage}
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
    </PageLayout>
  );
}

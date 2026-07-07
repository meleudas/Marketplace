"use client";

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
import {
  mapProductToRailCard,
  type ProductRailCard,
} from "@/features/home/lib/map-product-to-rail-card";
import { CatalogMenu, PageLayout, ProductCard, Spinner } from "@/shared/ui";
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

  const loadFeed = useCallback(
    async (page: number, categoryId: number | null, append: boolean) => {
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
        // ignore — keep current items
      } finally {
        setFeedLoading(false);
      }
    },
    [],
  );

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
    [feedLoading, hasMore, feedPage, selectedCategory, loadFeed],
  );

  useEffect(() => {
    let cancelled = false;

    const loadRecommendations = async () => {
      try {
        setRecommendationsLoading(true);
        const result = await getPersonalizedRecommendations({
          limit: HOME_RAIL_PAGE_SIZE,
        });

        if (!cancelled) {
          setRecommendations(result.items.map(mapProductToRailCard));
        }
      } catch {
        if (!cancelled) {
          setRecommendations([]);
        }
      } finally {
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

      const [popularResult, newResult, onSaleResult] = await Promise.allSettled(
        [
          getCatalogPopularProducts({ pageSize: HOME_RAIL_PAGE_SIZE }),
          getCatalogNewProducts({ pageSize: HOME_RAIL_PAGE_SIZE }),
          getCatalogOnSaleProducts({ pageSize: HOME_RAIL_PAGE_SIZE }),
        ],
      );

      if (cancelled) {
        return;
      }

      setPopular(
        popularResult.status === "fulfilled"
          ? popularResult.value.items.map(mapProductToRailCard)
          : [],
      );
      setNewProducts(
        newResult.status === "fulfilled"
          ? newResult.value.items.map(mapProductToRailCard)
          : [],
      );
      setOnSale(
        onSaleResult.status === "fulfilled"
          ? onSaleResult.value.items.map(mapProductToRailCard)
          : [],
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
          <p className={styles.promoQuote}>
            {"“"}Літературне чаювання{"”"}
          </p>
          <p className={styles.promoDate}>
            11:00 - Середа
            <br />
            15 квітня 2026
          </p>
        </div>
        <Image
          className={styles.promoCat}
          src="/promo-cat.svg"
          alt=""
          width={85}
          height={98}
        />
      </section>

      <RecommendationsRail
        title="Рекомендовані"
        loading={recommendationsLoading}
        viewAllHref="/catalog"
      >
        <ProductRailItems items={recommendations} />
      </RecommendationsRail>

      <RecommendationsRail
        title="Популярні"
        loading={popularLoading}
        viewAllHref="/catalog?sort=relevance"
      >
        <ProductRailItems items={popular} />
      </RecommendationsRail>

      <RecommendationsRail
        title="Новинки"
        loading={newProductsLoading}
        viewAllHref="/catalog?sort=newest"
      >
        <ProductRailItems items={newProducts} />
      </RecommendationsRail>

      <RecommendationsRail
        title="Акції"
        loading={onSaleLoading}
        viewAllHref="/catalog"
      >
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
              <Link
                key={item.id}
                href={item.href}
                className={styles.feedCardLink}
              >
                <ProductCard product={item} />
              </Link>
            ))}
          </div>
        ) : !feedLoading ? (
          <div className={styles.feedEmpty}>Немає товарів у цій категорії</div>
        ) : null}

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
          const formatParam =
            format === "all" ? "" : `?format=${encodeURIComponent(format)}`;
          router.push(`/catalog/${slug}${formatParam}`);
        }}
        onShowAll={(format) => {
          const formatParam =
            format === "all" ? "" : `?format=${encodeURIComponent(format)}`;
          router.push(`/catalog${formatParam}`);
        }}
      />
    </PageLayout>
  );
}

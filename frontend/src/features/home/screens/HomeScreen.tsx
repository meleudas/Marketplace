"use client";

import Image from "next/image";
import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
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
import { useAuth } from "@/features/auth/model/auth.store";
import { ChevronDownIcon, Button, CatalogMenu, PageLayout, ProductCard, SideDecorShell, Spinner } from "@/shared/ui";
import { ProductRailItems } from "../ui/ProductRailItems";
import { RecommendationsRail } from "../ui/RecommendationsRail";
import styles from "./HomeScreen.module.css";

const HOME_RAIL_PAGE_SIZE = 15;
const FEED_PAGE_SIZE = 24;

export function HomeScreen() {
  const router = useRouter();
  const isAuthenticated = useAuth((state) => state.isAuthenticated);
  const authInitialized = useAuth((state) => state.initialized);
  const loadMe = useAuth((state) => state.loadMe);
  const [catalogOpen, setCatalogOpen] = useState(false);
  const [categories, setCategories] = useState<CatalogCategoryDto[]>([]);
  const [recommendations, setRecommendations] = useState<ProductRailCard[]>([]);
  const [popular, setPopular] = useState<ProductRailCard[]>([]);
  const [newProducts, setNewProducts] = useState<ProductRailCard[]>([]);
  const [onSale, setOnSale] = useState<ProductRailCard[]>([]);
  const [recommendationsLoading, setRecommendationsLoading] = useState(false);
  const [popularLoading, setPopularLoading] = useState(true);
  const [newProductsLoading, setNewProductsLoading] = useState(true);
  const [onSaleLoading, setOnSaleLoading] = useState(true);

  const [feedItems, setFeedItems] = useState<ProductRailCard[]>([]);
  const [feedLoading, setFeedLoading] = useState(true);
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
          pageSize: FEED_PAGE_SIZE,
        });
        const newItems = response.items.map(mapProductToRailCard);

        setFeedItems((prev) => (append ? [...prev, ...newItems] : newItems));
        setHasMore(response.items.length === FEED_PAGE_SIZE);
      } catch {
        if (!append) {
          setFeedItems([]);
        }
        setHasMore(false);
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
    void loadMe();
  }, [loadMe]);

  useEffect(() => {
    setFeedPage(1);
    setHasMore(true);
    void loadFeed(1, selectedCategory, false);
  }, [selectedCategory, loadFeed]);

  const handleLoadMoreFeed = () => {
    if (feedLoading || !hasMore) {
      return;
    }

    const nextPage = feedPage + 1;
    setFeedPage(nextPage);
    void loadFeed(nextPage, selectedCategory, true);
  };

  useEffect(() => {
    if (!authInitialized || !isAuthenticated) {
      setRecommendations([]);
      setRecommendationsLoading(false);
      return;
    }

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
  }, [authInitialized, isAuthenticated]);

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
      className={styles.homeMain}
      headerProps={{
        homeHref: "/",
        userHref: "/me",
        onMenuClick: () => setCatalogOpen(true),
      }}
      footerProps={{ homeHref: "/" }}
    >
      <SideDecorShell contentClassName={styles.homeContent}>
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

      {authInitialized && isAuthenticated ? (
        <RecommendationsRail
          title="Рекомендовані"
          variant="personal"
          loading={recommendationsLoading}
          viewAllHref="/catalog"
        >
          <ProductRailItems items={recommendations} />
        </RecommendationsRail>
      ) : null}

      <RecommendationsRail
        title="Популярні"
        variant="popular"
        loading={popularLoading}
        viewAllHref="/catalog?sort=relevance"
      >
        <ProductRailItems items={popular} />
      </RecommendationsRail>

      <RecommendationsRail
        title="Новинки"
        variant="new"
        loading={newProductsLoading}
        viewAllHref="/catalog?sort=newest"
      >
        <ProductRailItems items={newProducts} />
      </RecommendationsRail>

      <RecommendationsRail
        title="Акції"
        variant="sale"
        loading={onSaleLoading}
        viewAllHref="/catalog"
      >
        <ProductRailItems items={onSale} />
      </RecommendationsRail>

      <section className={styles.discountPromo} aria-label="Знижка на перше замовлення">
        <span className={styles.discountPromoFrame}>
          <Image
            src="/first-order-discount.png"
            alt="Отримай -15% на перше замовлення. Реєструйся та отримуй більше оновлення від придбання книг."
            className={styles.discountPromoImage}
            width={706}
            height={424}
            sizes="(max-width: 480px) 100vw, (max-width: 1023px) 480px, 560px"
          />
        </span>
      </section>

      <section className={styles.feedSection} aria-label="Спеціально для вас">
        <div className={styles.feedHeader}>
          <h2 className={styles.feedTitle}>Спеціально для вас</h2>

          <div className={styles.tagPillsList} role="tablist">
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

        {!feedLoading && hasMore && feedItems.length > 0 ? (
          <div className={styles.loadMoreWrap}>
            <Button
              variant="secondary"
              size="lg"
              onClick={handleLoadMoreFeed}
              leadingIcon={<ChevronDownIcon className={styles.loadMoreIcon} />}
            >
              Показати ще
            </Button>
          </div>
        ) : null}
      </section>
      </SideDecorShell>

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

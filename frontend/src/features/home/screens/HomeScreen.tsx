"use client";

import Image from "next/image";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import {
  getCatalogCategories,
  getCatalogNewProducts,
  getCatalogOnSaleProducts,
  getCatalogPopularProducts,
  getPersonalizedRecommendations,
} from "@/features/storefront/api/catalog.api";
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
        homeHref: "/home",
        userHref: "/me",
        onMenuClick: () => setCatalogOpen(true),
      }}
      footerProps={{ homeHref: "/home" }}
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

        <Image className={styles.promoCat} src="/promo-cat.svg" alt="" width={85} height={98} />
      </section>

      <RecommendationsRail title="Рекомендовані" loading={recommendationsLoading}>
        <ProductRailItems items={recommendations} />
      </RecommendationsRail>

      <RecommendationsRail title="Популярні" loading={popularLoading}>
        <ProductRailItems items={popular} />
      </RecommendationsRail>

      <RecommendationsRail title="Новинки" loading={newProductsLoading}>
        <ProductRailItems items={newProducts} />
      </RecommendationsRail>

      <RecommendationsRail title="Акції" loading={onSaleLoading}>
        <ProductRailItems items={onSale} />
      </RecommendationsRail>

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
        onCategorySelect={(slug) => router.push(`/catalog/${slug}`)}
        onShowAll={() => router.push("/catalog")}
      />
    </PageLayout>
  );
}

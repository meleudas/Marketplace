"use client";

import Image from "next/image";
import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import {
  getCatalogCategories,
  getCatalogProductBySlug,
  getPersonalizedRecommendations,
} from "@/features/storefront/api/catalog.api";
import type { CatalogCategoryDto, CatalogProductListItemDto } from "@/features/storefront/model/catalog.types";
import { CatalogMenu, PageLayout, ProductCard, type ProductCardData } from "@/shared/ui";
import { RecommendationsRail } from "../ui/RecommendationsRail";
import styles from "./HomeScreen.module.css";

interface RecommendationCard extends ProductCardData {
  href: string;
}

export function HomeScreen() {
  const router = useRouter();
  const [catalogOpen, setCatalogOpen] = useState(false);
  const [categories, setCategories] = useState<CatalogCategoryDto[]>([]);
  const [recommendations, setRecommendations] = useState<RecommendationCard[]>([]);
  const [recommendationsLoading, setRecommendationsLoading] = useState(true);

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
        const result = await getPersonalizedRecommendations({ limit: 12 });
        const items = await Promise.all(
          result.items.map(async (product: CatalogProductListItemDto) => {
            try {
              const details = await getCatalogProductBySlug(product.slug);
              const imageUrl = details.images[0]?.thumbnailUrl ?? details.images[0]?.imageUrl ?? null;

              return {
                id: String(product.id),
                imageUrl,
                inStock: product.availabilityStatus !== "out_of_stock" && product.availableQty > 0,
                price: product.price,
                title: product.name,
                href: `/products/${product.slug}`,
              } satisfies RecommendationCard;
            } catch {
              return {
                id: String(product.id),
                imageUrl: null,
                inStock: product.availabilityStatus !== "out_of_stock" && product.availableQty > 0,
                price: product.price,
                title: product.name,
                href: `/products/${product.slug}`,
              } satisfies RecommendationCard;
            }
          }),
        );

        if (!cancelled) {
          setRecommendations(items);
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
        {recommendations.map((item) => (
          <Link
            key={item.id}
            href={item.href}
            className={styles.recommendationCard}
            role="listitem"
          >
            <ProductCard
              product={{
                id: item.id,
                title: item.title,
                price: item.price,
                imageUrl: item.imageUrl ?? undefined,
                inStock: item.inStock,
              }}
            />
          </Link>
        ))}
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

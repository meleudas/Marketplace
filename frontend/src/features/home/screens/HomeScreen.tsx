"use client";

import Image from "next/image";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { getCatalogCategories } from "@/features/storefront/api/catalog.api";
import type { CatalogCategoryDto } from "@/features/storefront/model/catalog.types";
import { CatalogMenu, PageLayout } from "@/shared/ui";
import styles from "./HomeScreen.module.css";

export function HomeScreen() {
  const router = useRouter();
  const [catalogOpen, setCatalogOpen] = useState(false);
  const [categories, setCategories] = useState<CatalogCategoryDto[]>([]);

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

"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { getCatalogCategories, getCatalogProducts } from "@/features/storefront/api/catalog.api";
import type { CatalogCategoryDto, CatalogProductListItemDto } from "@/features/storefront/model/catalog.types";
import { CategoryList } from "@/features/storefront/ui/CategoryList";
import { ProductCard } from "@/features/storefront/ui/ProductCard";
import { StateBlock } from "@/features/storefront/ui/StateBlock";
import { StorefrontLayout } from "@/features/storefront/ui/StorefrontLayout";
import styles from "./StorefrontScreen.module.css";

const HOME_PRODUCTS_LIMIT = 6;

export function HomeScreen() {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [categories, setCategories] = useState<CatalogCategoryDto[]>([]);
  const [products, setProducts] = useState<CatalogProductListItemDto[]>([]);

  useEffect(() => {
    const load = async () => {
      try {
        setLoading(true);
        setError(null);

        const [categoriesData, productsData] = await Promise.all([
          getCatalogCategories(),
          getCatalogProducts(),
        ]);

        setCategories(categoriesData);
        setProducts(productsData.slice(0, HOME_PRODUCTS_LIMIT));
      } catch {
        setError("Failed to load data");
      } finally {
        setLoading(false);
      }
    };

    void load();
  }, []);

  return (
    <StorefrontLayout title="Marketplace Storefront">
      <section className={styles.hero}>
        <h2 className={styles.heroTitle}>Marketplace</h2>
        <p className={styles.heroText}>
          Minimal public storefront connected to real catalog API endpoints.
        </p>

        <div className={styles.actions}>
          <Link href="/products" className={styles.actionLink}>
            View all products
          </Link>
          <Link href="/companies" className={styles.actionLink}>
            View companies
          </Link>
        </div>
      </section>

      {loading ? <StateBlock message="Loading..." /> : null}
      {error ? <StateBlock message={error} isError /> : null}

      {!loading && !error ? (
        <>
          <CategoryList categories={categories} />

          <section className={styles.section}>
            <h2 className={styles.sectionTitle}>Products</h2>
            {products.length === 0 ? (
              <StateBlock message="No products found" />
            ) : (
              <div className={styles.grid}>
                {products.map((product) => (
                  <ProductCard key={product.id} product={product} />
                ))}
              </div>
            )}
          </section>
        </>
      ) : null}
    </StorefrontLayout>
  );
}


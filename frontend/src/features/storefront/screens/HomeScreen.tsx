"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { getCatalogProducts } from "@/features/storefront/api/catalog.api";
import type { CatalogProductListItemDto } from "@/features/storefront/model/catalog.types";
import { StateBlock } from "@/features/storefront/ui/StateBlock";
import { Grid, PageLayout, ProductCard, Typography } from "@/shared/ui";
import type { ProductCardData } from "@/shared/ui/ProductCard";
import styles from "./HomeScreen.module.css";

const HOME_PRODUCTS_LIMIT = 6;

const mapProductToCard = (product: CatalogProductListItemDto): ProductCardData => ({
  id: String(product.id),
  title: product.name,
  price: product.price,
  inStock: product.availabilityStatus !== "out_of_stock" && product.availableQty > 0,
});

export function HomeScreen() {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [products, setProducts] = useState<CatalogProductListItemDto[]>([]);

  useEffect(() => {
    const load = async () => {
      try {
        setLoading(true);
        setError(null);

        const productsData = await getCatalogProducts();
        setProducts(productsData.slice(0, HOME_PRODUCTS_LIMIT));
      } catch {
        setError("Не вдалося завантажити товари");
      } finally {
        setLoading(false);
      }
    };

    void load();
  }, []);

  return (
    <PageLayout headerProps={{ homeHref: "/home", userHref: "/me" }} footerProps={{ homeHref: "/home" }}>
      <header className={styles.section}>
        <Typography variant="h1">BOOK TOP</Typography>
        <Typography variant="body1" className={styles.muted}>
          Книжковий маркетплейс — обирайте улюблені видання від перевірених продавців.
        </Typography>
      </header>

      <section className={styles.section}>
        <div className={styles.sectionHeader}>
          <Typography variant="h2" className={styles.sectionTitle}>
            Популярні товари
          </Typography>
          <Link href="/products" className={styles.sectionLink}>
            Усі товари
          </Link>
        </div>

        {loading ? <StateBlock message="Завантаження..." /> : null}
        {error ? <StateBlock message={error} isError /> : null}

        {!loading && !error && products.length === 0 ? (
          <StateBlock message="Товарів поки немає" />
        ) : null}

        {!loading && !error && products.length > 0 ? (
          <Grid layout="auto" minColumnWidth="10rem" gap="md">
            {products.map((product) => (
              <Link
                key={product.id}
                href={`/products/${product.slug}`}
                className={styles.cardLink}
              >
                <ProductCard product={mapProductToCard(product)} />
              </Link>
            ))}
          </Grid>
        ) : null}
      </section>
    </PageLayout>
  );
}

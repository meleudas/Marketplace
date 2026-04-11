"use client";

import { useEffect, useMemo, useState } from "react";
import { getCatalogCategories, getCatalogProducts } from "@/features/storefront/api/catalog.api";
import type { CatalogCategoryDto, CatalogProductListItemDto } from "@/features/storefront/model/catalog.types";
import { CategoryList } from "@/features/storefront/ui/CategoryList";
import { ProductCard } from "@/features/storefront/ui/ProductCard";
import { StateBlock } from "@/features/storefront/ui/StateBlock";
import { StorefrontLayout } from "@/features/storefront/ui/StorefrontLayout";
import styles from "./StorefrontScreen.module.css";

const productMatchesCategory = (
  product: CatalogProductListItemDto,
  category: CatalogCategoryDto,
): boolean => {
  return product.categoryId === category.id;
};

export function ProductsScreen() {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedCategorySlug, setSelectedCategorySlug] = useState<string | null>(null);
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
        setProducts(productsData);
      } catch {
        setError("Failed to load data");
      } finally {
        setLoading(false);
      }
    };

    void load();
  }, []);

  const filteredProducts = useMemo(() => {
    if (!selectedCategorySlug) {
      return products;
    }

    const selectedCategory = categories.find((category) => category.slug === selectedCategorySlug);
    if (!selectedCategory) {
      return products;
    }

    const matches = products.filter((product) => productMatchesCategory(product, selectedCategory));

    // If API does not expose category linkage in product items, keep products visible.
    return matches.length > 0 ? matches : products;
  }, [categories, products, selectedCategorySlug]);

  return (
    <StorefrontLayout title="Products">
      {loading ? <StateBlock message="Loading..." /> : null}
      {error ? <StateBlock message={error} isError /> : null}

      {!loading && !error ? (
        <>
          <CategoryList
            categories={categories}
            selectedSlug={selectedCategorySlug}
            onSelect={setSelectedCategorySlug}
          />

          <section className={styles.section}>
            <h2 className={styles.sectionTitle}>Catalog products</h2>
            {filteredProducts.length === 0 ? (
              <StateBlock message="No products found" />
            ) : (
              <div className={styles.grid}>
                {filteredProducts.map((product) => (
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

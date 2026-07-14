"use client";

import type { ReactNode } from "react";
import { useAddToCart } from "@/features/cart/hooks/useAddToCart";
import { AddToCartDialog } from "@/features/cart/ui/AddToCartDialog";
import { mapCatalogProductToCardData } from "@/features/storefront/lib/map-catalog-product-to-card";
import type { CatalogProductListItemDto } from "@/features/storefront/model/catalog.types";
import { Pagination, ProductCard, ProductCardSkeleton } from "@/shared/ui";
import styles from "../screens/CatalogScreen.module.css";

interface CatalogProductGridProps {
  loading: boolean;
  refreshing: boolean;
  error: string | null;
  products: CatalogProductListItemDto[];
  pageSize: number;
  totalPages: number;
  currentPage: number;
  onPageChange: (page: number) => void;
}

export function CatalogProductGrid({
  loading,
  refreshing,
  error,
  products,
  pageSize,
  totalPages,
  currentPage,
  onPageChange,
}: CatalogProductGridProps) {
  const { addToCart, addingProductId, addedProduct, dismissAddedDialog } = useAddToCart();

  let content: ReactNode;

  if (loading) {
    content = (
      <div className={styles.skeletonGrid} aria-hidden="true">
        {Array.from({ length: pageSize }, (_, index) => (
          <ProductCardSkeleton key={index} />
        ))}
      </div>
    );
  } else if (refreshing) {
    content = null;
  } else if (!error && products.length === 0) {
    content = (
      <p className={styles.emptyState} role="status">
        Товарів за обраними фільтрами не знайдено
      </p>
    );
  } else if (error || products.length === 0) {
    content = null;
  } else {
    content = (
      <>
        <div className={styles.productGrid}>
          {products.map((product) => (
            <div key={product.id} className={styles.cardLink}>
              <ProductCard
                product={mapCatalogProductToCardData(product)}
                href={`/products/${product.slug}`}
                onAddToCart={addToCart}
                isAddingToCart={addingProductId === String(product.id)}
              />
            </div>
          ))}
        </div>

        {totalPages > 1 ? (
          <div className={styles.paginationWrap}>
            <Pagination
              currentPage={currentPage}
              totalPages={totalPages}
              onPageChange={onPageChange}
            />
          </div>
        ) : null}
      </>
    );
  }

  return (
    <>
      {content}
      <AddToCartDialog
        open={addedProduct !== null}
        product={addedProduct}
        onClose={dismissAddedDialog}
      />
    </>
  );
}

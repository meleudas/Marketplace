import Link from "next/link";
import { mapCatalogProductToCardData } from "@/features/storefront/lib/map-catalog-product-to-card";
import type { CatalogProductListItemDto } from "@/features/storefront/model/catalog.types";
import { Pagination, ProductCard, ProductCardSkeleton } from "@/shared/ui";
import styles from "../screens/CatalogScreen.module.css";

interface CatalogProductGridProps {
  loading: boolean;
  refreshing: boolean;
  error: string | null;
  products: CatalogProductListItemDto[];
  totalPages: number;
  currentPage: number;
  onPageChange: (page: number) => void;
}

export function CatalogProductGrid({
  loading,
  refreshing,
  error,
  products,
  totalPages,
  currentPage,
  onPageChange,
}: CatalogProductGridProps) {
  if (loading) {
    return (
      <div className={styles.skeletonGrid} aria-hidden="true">
        {Array.from({ length: 8 }, (_, index) => (
          <ProductCardSkeleton key={index} />
        ))}
      </div>
    );
  }

  if (refreshing) {
    return null;
  }

  if (!error && products.length === 0) {
    return (
      <p className={styles.emptyState} role="status">
        Товарів за обраними фільтрами не знайдено
      </p>
    );
  }

  if (error || products.length === 0) {
    return null;
  }

  return (
    <>
      <div className={styles.productGrid}>
        {products.map((product) => (
          <Link key={product.id} href={`/products/${product.slug}`} className={styles.cardLink}>
            <ProductCard product={mapCatalogProductToCardData(product)} />
          </Link>
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

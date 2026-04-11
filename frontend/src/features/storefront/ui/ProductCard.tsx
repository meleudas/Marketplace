import Link from "next/link";
import type { CatalogProductListItemDto } from "@/features/storefront/model/catalog.types";
import styles from "./ProductCard.module.css";

interface ProductCardProps {
  product: CatalogProductListItemDto;
}

const formatPrice = (price: number | null | undefined): string => {
  if (typeof price !== "number") {
    return "-";
  }

  return `$${price.toFixed(2)}`;
};

export function ProductCard({ product }: ProductCardProps) {
  return (
    <Link href={`/products/${product.slug}`} className={styles.cardLink}>
      <article className={styles.card}>
        <div className={styles.body}>
          <h3 className={styles.title}>{product.name}</h3>

          <p className={styles.meta}>Slug: {product.slug}</p>

          <div className={styles.priceRow}>
            <span className={styles.price}>{formatPrice(product.price)}</span>
            {typeof product.oldPrice === "number" ? (
              <span className={styles.oldPrice}>{formatPrice(product.oldPrice)}</span>
            ) : null}
          </div>

          <p className={styles.meta}>Available: {product.availableQty}</p>
          <p className={styles.meta}>Status: {product.availabilityStatus}</p>
        </div>
      </article>
    </Link>
  );
}


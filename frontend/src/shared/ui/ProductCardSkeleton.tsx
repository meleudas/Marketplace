import styles from "./ProductCardSkeleton.module.css";

interface ProductCardSkeletonProps {
  className?: string;
}

export function ProductCardSkeleton({ className }: ProductCardSkeletonProps) {
  return (
    <article className={[styles.card, className].filter(Boolean).join(" ")}>
      <div className={styles.media}>
        <div className={styles.shimmer} />
      </div>

      <div className={styles.body}>
        <div className={styles.lineLg} />
        <div className={styles.lineSm} />
        <div className={styles.priceRow}>
          <div className={styles.linePrice} />
          <div className={styles.button} />
        </div>
      </div>
    </article>
  );
}

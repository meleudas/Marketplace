import { ProductCardSkeleton } from "@/shared/ui";
import styles from "./RecommendationsRail.module.css";
import type { ReactNode } from "react";

interface RecommendationsRailProps {
  title: string;
  loading?: boolean;
  children?: ReactNode;
}

export function RecommendationsRail({ title, loading = false, children }: RecommendationsRailProps) {
  if (!loading && !children) {
    return null;
  }

  return (
    <section className={styles.section} aria-label={title}>
      <h2 className={styles.title}>{title}</h2>

      <div className={styles.scroller} role="list">
        {loading
          ? Array.from({ length: 4 }, (_, index) => (
              <div key={index} className={styles.cardLink} role="listitem" aria-hidden="true">
                <ProductCardSkeleton className={styles.skeletonCard} />
              </div>
            ))
          : children}
      </div>
    </section>
  );
}

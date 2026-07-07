import Link from "next/link";
import { ProductCardSkeleton } from "@/shared/ui";
import styles from "./RecommendationsRail.module.css";
import type { ReactNode } from "react";

interface RecommendationsRailProps {
  title: string;
  loading?: boolean;
  viewAllHref?: string;
  children?: ReactNode;
}

export function RecommendationsRail({ title, loading = false, viewAllHref, children }: RecommendationsRailProps) {
  if (!loading && !children) {
    return null;
  }

  return (
    <section className={styles.section} aria-label={title}>
      <div className={styles.headerRow}>
        <h2 className={styles.title}>{title}</h2>
        {viewAllHref && (
          <Link href={viewAllHref} className={styles.viewAllBtn}>
            Всі
          </Link>
        )}
      </div>

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

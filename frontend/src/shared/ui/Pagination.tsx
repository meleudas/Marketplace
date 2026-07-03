import type { MouseEvent } from "react";
import styles from "./Pagination.module.css";

interface PaginationProps {
  currentPage: number;
  totalPages: number;
  onPageChange?: (page: number) => void;
}

/** Presentational pagination. Builds a simple page range with ellipsis. */
function buildPages(current: number, total: number): (number | "...")[] {
  if (total <= 5) {
    return Array.from({ length: total }, (_, i) => i + 1);
  }

  if (current <= 3) {
    return [1, 2, 3, "...", total];
  }

  if (current >= total - 2) {
    return [1, "...", total - 2, total - 1, total];
  }

  return [1, "...", current, "...", total];
}

function isPageNumber(page: number | "..."): page is number {
  return page !== "...";
}

const preventFocusScroll = (event: MouseEvent<HTMLButtonElement>) => {
  event.preventDefault();
};

export function Pagination({ currentPage, totalPages, onPageChange }: PaginationProps) {
  const pages = buildPages(currentPage, totalPages);

  return (
    <nav className={styles.pagination} aria-label="Пагінація">
      <button
        type="button"
        className={styles.arrow}
        disabled={currentPage <= 1}
        onMouseDown={preventFocusScroll}
        onClick={() => onPageChange?.(currentPage - 1)}
        aria-label="Попередня сторінка"
      >
        ‹
      </button>

      {pages.map((page, index) =>
        !isPageNumber(page) ? (
          <span key={`gap-${index}`} className={styles.ellipsis} aria-hidden="true">
            …
          </span>
        ) : (
          <button
            key={page}
            type="button"
            className={`${styles.page} ${page === currentPage ? styles.active : ""}`}
            aria-current={page === currentPage ? "page" : undefined}
            onMouseDown={preventFocusScroll}
            onClick={() => onPageChange?.(page)}
          >
            {page}
          </button>
        ),
      )}

      <button
        type="button"
        className={styles.arrow}
        disabled={currentPage >= totalPages}
        onMouseDown={preventFocusScroll}
        onClick={() => onPageChange?.(currentPage + 1)}
        aria-label="Наступна сторінка"
      >
        ›
      </button>
    </nav>
  );
}

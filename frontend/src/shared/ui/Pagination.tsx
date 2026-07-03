import type { MouseEvent } from "react";
import styles from "./Pagination.module.css";

interface PaginationProps {
  currentPage: number;
  totalPages: number;
  onPageChange?: (page: number) => void;
}

/** Builds a page range with ellipsis, always keeping current and adjacent pages visible. */
function buildPages(current: number, total: number): (number | "...")[] {
  if (total <= 7) {
    return Array.from({ length: total }, (_, index) => index + 1);
  }

  const pages: (number | "...")[] = [];
  const siblingCount = 1;
  const rangeStart = Math.max(2, current - siblingCount);
  const rangeEnd = Math.min(total - 1, current + siblingCount);

  pages.push(1);

  if (rangeStart > 2) {
    pages.push("...");
  }

  for (let page = rangeStart; page <= rangeEnd; page += 1) {
    pages.push(page);
  }

  if (rangeEnd < total - 1) {
    pages.push("...");
  }

  pages.push(total);

  return pages;
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
            className={`${styles.page} ${page === currentPage ? styles.active : ""}`.trim()}
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

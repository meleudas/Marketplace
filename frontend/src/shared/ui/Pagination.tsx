import styles from "./Pagination.module.css";

interface PaginationProps {
  currentPage: number;
  totalPages: number;
  onPageChange?: (page: number) => void;
}

/** Presentational pagination. Builds a simple page range with ellipsis. */
function buildPages(current: number, total: number): (number | "...")[] {
  if (total <= 7) {
    return Array.from({ length: total }, (_, i) => i + 1);
  }

  const pages: (number | "...")[] = [1];
  const start = Math.max(2, current - 1);
  const end = Math.min(total - 1, current + 1);

  if (start > 2) pages.push("...");
  for (let page = start; page <= end; page += 1) pages.push(page);
  if (end < total - 1) pages.push("...");

  pages.push(total);
  return pages;
}

export function Pagination({ currentPage, totalPages, onPageChange }: PaginationProps) {
  const pages = buildPages(currentPage, totalPages);

  return (
    <nav className={styles.pagination} aria-label="Пагінація">
      <button
        type="button"
        className={styles.arrow}
        disabled={currentPage <= 1}
        onClick={() => onPageChange?.(currentPage - 1)}
        aria-label="Попередня сторінка"
      >
        ‹
      </button>

      {pages.map((page, index) =>
        page === "..." ? (
          <span key={`gap-${index}`} className={styles.ellipsis} aria-hidden="true">
            …
          </span>
        ) : (
          <button
            key={page}
            type="button"
            className={`${styles.page} ${page === currentPage ? styles.active : ""}`}
            aria-current={page === currentPage ? "page" : undefined}
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
        onClick={() => onPageChange?.(currentPage + 1)}
        aria-label="Наступна сторінка"
      >
        ›
      </button>
    </nav>
  );
}

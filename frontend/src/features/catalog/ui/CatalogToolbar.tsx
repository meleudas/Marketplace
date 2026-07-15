import type { CatalogProductSort } from "@/features/storefront/lib/catalog-product-sort";
import { ArrowsSortIcon, Button, FilterIcon } from "@/shared/ui";
import iconStyles from "@/shared/ui/icons/Icon.module.css";
import styles from "../screens/CatalogScreen.module.css";
import { CatalogSortDropdown } from "./CatalogSortDropdown";

interface CatalogToolbarProps {
  loading: boolean;
  filtersOpen: boolean;
  sortModalOpen: boolean;
  sortButtonLabel: string;
  totalCount: number;
  selectedSort: CatalogProductSort;
  onOpenFilters: () => void;
  onToggleSort: () => void;
  onSelectSort: (sort: CatalogProductSort) => void;
}

export function CatalogToolbar({
  loading,
  filtersOpen,
  sortModalOpen,
  sortButtonLabel,
  totalCount,
  selectedSort,
  onOpenFilters,
  onToggleSort,
  onSelectSort,
}: CatalogToolbarProps) {
  return (
    <>
      <div className={styles.toolbar}>
        {loading ? (
          <div className={styles.skeletonToolbar} aria-hidden="true">
            <div className={styles.skeletonButton} />
            <div className={styles.skeletonButton} />
          </div>
        ) : (
          <>
            <Button
              type="button"
              variant="dark"
              size="lg"
              fullWidth
              leadingIcon={<FilterIcon className={iconStyles.icon} />}
              aria-haspopup="dialog"
              aria-expanded={filtersOpen}
              onClick={onOpenFilters}
            >
              Фільтри
            </Button>

            <Button
              type="button"
              variant="dark"
              size="lg"
              fullWidth
              leadingIcon={<ArrowsSortIcon className={iconStyles.icon} />}
              aria-haspopup="dialog"
              aria-expanded={sortModalOpen}
              onClick={onToggleSort}
            >
              {sortButtonLabel}
            </Button>
          </>
        )}
      </div>

      <div className={styles.desktopToolbar}>
        {loading ? (
          <div className={styles.skeletonDesktopToolbar} aria-hidden="true" />
        ) : (
          <>
            <p className={styles.resultsCount}>
              Знайдено {totalCount} {totalCount === 1 ? "товар" : "товарів"}
            </p>
            <CatalogSortDropdown selectedSort={selectedSort} onSelect={onSelectSort} />
          </>
        )}
      </div>
    </>
  );
}

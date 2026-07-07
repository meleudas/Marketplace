import { ArrowsSortIcon, Button, FilterIcon } from "@/shared/ui";
import iconStyles from "@/shared/ui/icons/Icon.module.css";
import styles from "../screens/CatalogScreen.module.css";

interface CatalogToolbarProps {
  loading: boolean;
  filtersOpen: boolean;
  sortModalOpen: boolean;
  sortButtonLabel: string;
  onOpenFilters: () => void;
  onToggleSort: () => void;
}

export function CatalogToolbar({
  loading,
  filtersOpen,
  sortModalOpen,
  sortButtonLabel,
  onOpenFilters,
  onToggleSort,
}: CatalogToolbarProps) {
  return (
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
  );
}

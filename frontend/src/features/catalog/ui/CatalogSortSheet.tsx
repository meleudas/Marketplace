import {
  CATALOG_PRODUCT_SORT_OPTIONS,
  type CatalogProductSort,
} from "@/features/storefront/lib/catalog-product-sort";
import styles from "../screens/CatalogScreen.module.css";

interface CatalogSortSheetProps {
  open: boolean;
  selectedSort: CatalogProductSort;
  onClose: () => void;
  onSelect: (sort: CatalogProductSort) => void;
}

export function CatalogSortSheet({
  open,
  selectedSort,
  onClose,
  onSelect,
}: CatalogSortSheetProps) {
  if (!open) {
    return null;
  }

  return (
    <>
      <button
        type="button"
        className={styles.sortBackdrop}
        aria-label="Закрити сортування"
        onClick={onClose}
      />
      <div className={styles.sortSheet} role="dialog" aria-modal="true" aria-label="Сортування">
        <div className={styles.sortSheetOptions}>
          {CATALOG_PRODUCT_SORT_OPTIONS.map((option) => {
            const isActive = selectedSort === option.value;

            return (
              <button
                key={option.value}
                type="button"
                className={isActive ? styles.sortOptionActive : styles.sortOption}
                aria-pressed={isActive}
                onClick={() => onSelect(option.value)}
              >
                {option.label}
              </button>
            );
          })}
        </div>
      </div>
    </>
  );
}

import { Button } from "@/shared/ui";
import type { CatalogCategoryDto } from "@/features/storefront/model/catalog.types";
import styles from "../screens/CatalogScreen.module.css";

interface CatalogCategoryTabsProps {
  loading: boolean;
  rootCategories: CatalogCategoryDto[];
  subcategories: CatalogCategoryDto[];
  selectedRootSlug: string | null;
  selectedSubcategorySlug: string | null;
  onRootCategoryClick: (slug: string) => void;
  onSubcategoryClick: (slug: string) => void;
}

export function CatalogCategoryTabs({
  loading,
  rootCategories,
  subcategories,
  selectedRootSlug,
  selectedSubcategorySlug,
  onRootCategoryClick,
  onSubcategoryClick,
}: CatalogCategoryTabsProps) {
  if (loading) {
    return (
      <div className={styles.skeletonRow} aria-hidden="true">
        {Array.from({ length: 4 }, (_, index) => (
          <div key={index} className={styles.skeletonChip} />
        ))}
      </div>
    );
  }

  if (rootCategories.length === 0) {
    return null;
  }

  return (
    <div className={styles.categoryRows}>
      <div className={styles.categories} role="tablist" aria-label="Основні категорії">
        {rootCategories.map((category) => {
          const isActive = selectedRootSlug === category.slug;

          return (
            <Button
              key={category.id}
              type="button"
              role="tab"
              variant="dark"
              size="sm"
              selectable
              selected={isActive}
              aria-selected={isActive}
              onClick={() => onRootCategoryClick(category.slug)}
            >
              {category.name}
            </Button>
          );
        })}
      </div>

      {subcategories.length > 0 ? (
        <div className={styles.subcategories} role="tablist" aria-label="Підкатегорії">
          {subcategories.map((category) => {
            const isActive = selectedSubcategorySlug === category.slug;

            return (
              <Button
                key={category.id}
                type="button"
                role="tab"
                variant="dark"
                size="sm"
                selectable
                selected={isActive}
                aria-selected={isActive}
                onClick={() => onSubcategoryClick(category.slug)}
              >
                {category.name}
              </Button>
            );
          })}
        </div>
      ) : null}
    </div>
  );
}

import type { CatalogCategoryDto } from "@/features/storefront/model/catalog.types";
import styles from "./CategoryList.module.css";

interface CategoryListProps {
  categories: CatalogCategoryDto[];
  selectedSlug?: string | null;
  onSelect?: (slug: string | null) => void;
}

export function CategoryList({ categories, selectedSlug, onSelect }: CategoryListProps) {
  return (
    <section className={styles.wrap}>
      <h2 className={styles.title}>Categories</h2>

      <div className={styles.list}>
        {onSelect ? (
          <button
            type="button"
            className={`${styles.button} ${!selectedSlug ? styles.buttonActive : ""}`.trim()}
            onClick={() => onSelect(null)}
          >
            All
          </button>
        ) : null}

        {categories.map((category) => (
          <button
            key={category.id}
            type="button"
            className={`${styles.button} ${selectedSlug === category.slug ? styles.buttonActive : ""}`.trim()}
            onClick={() => onSelect?.(category.slug)}
            disabled={!onSelect}
          >
            {category.name}
          </button>
        ))}
      </div>
    </section>
  );
}


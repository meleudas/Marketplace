import { FORMAT_FILTER_OPTIONS } from "@/features/catalog/lib/catalog-filter-options";
import type { CatalogCategoryDto, CatalogFacetOptionDto } from "@/features/storefront/model/catalog.types";
import { CloseIcon } from "@/shared/ui";
import buttonStyles from "@/shared/ui/Button/Button.module.css";
import iconStyles from "@/shared/ui/icons/Icon.module.css";
import styles from "../screens/CatalogScreen.module.css";

interface CatalogSelectedFiltersProps {
  loading: boolean;
  categories: CatalogCategoryDto[];
  authorOptions: CatalogFacetOptionDto[];
  routeCategorySlugs: string[];
  appliedCategorySlugs: string[];
  appliedAuthors: string[];
  appliedFormat: string | null;
  onRemoveCategory: (slug: string, source: "applied" | "route") => void;
  onRemoveAuthor: (author: string) => void;
  onRemoveFormat: () => void;
}

type SelectedFilterItem =
  | { key: string; label: string; type: "category"; value: string; source: "applied" | "route" }
  | { key: string; label: string; type: "author"; value: string }
  | { key: string; label: string; type: "format"; value: string };

export function CatalogSelectedFilters({
  loading,
  categories,
  authorOptions = [],
  routeCategorySlugs,
  appliedCategorySlugs,
  appliedAuthors,
  appliedFormat,
  onRemoveCategory,
  onRemoveAuthor,
  onRemoveFormat,
}: CatalogSelectedFiltersProps) {
  if (loading) {
    return (
      <div className={styles.skeletonRow} aria-hidden="true">
        {Array.from({ length: 4 }, (_, index) => (
          <div key={index} className={styles.skeletonChip} />
        ))}
      </div>
    );
  }

  const categorySource = appliedCategorySlugs.length > 0 ? "applied" : "route";
  const categorySlugs =
    appliedCategorySlugs.length > 0 ? appliedCategorySlugs : routeCategorySlugs;
  const categoryItems = categorySlugs
    .map((slug) => categories.find((category) => category.slug === slug))
    .filter((category): category is CatalogCategoryDto => Boolean(category))
    .map<SelectedFilterItem>((category) => ({
      key: `category-${category.slug}`,
      label: category.name,
      type: "category",
      value: category.slug,
      source: categorySource,
    }));
  const authorItems = appliedAuthors.map<SelectedFilterItem>((author) => ({
    key: `author-${author}`,
    label: authorOptions.find((option) => option.value === author)?.label ?? author,
    type: "author",
    value: author,
  }));
  const formatOption = appliedFormat
    ? FORMAT_FILTER_OPTIONS.find((option) => option.value === appliedFormat)
    : null;
  const formatItems: SelectedFilterItem[] = formatOption
    ? [
        {
          key: `format-${formatOption.value}`,
          label: formatOption.label,
          type: "format",
          value: formatOption.value,
        },
      ]
    : [];
  const items = [...categoryItems, ...authorItems, ...formatItems];

  if (items.length === 0) {
    return null;
  }

  const handleRemove = (item: SelectedFilterItem) => {
    switch (item.type) {
      case "category":
        onRemoveCategory(item.value, item.source);
        break;
      case "author":
        onRemoveAuthor(item.value);
        break;
      case "format":
        onRemoveFormat();
        break;
    }
  };

  return (
    <section className={styles.selectedFilters} aria-label="Обрані фільтри">
      <h2 className={styles.selectedFiltersTitle}>Обрані фільтри</h2>
      <div className={styles.selectedFilterList}>
        {items.map((item) => (
          <div
            key={item.key}
            className={[
              buttonStyles.button,
              buttonStyles.dark,
              buttonStyles.sm,
              buttonStyles.selectable,
              styles.selectedFilterChip,
            ].join(" ")}
          >
            <span className={buttonStyles.label}>{item.label}</span>
            <button
              type="button"
              className={styles.selectedFilterRemove}
              aria-label={`Прибрати фільтр ${item.label}`}
              onClick={() => handleRemove(item)}
            >
              <CloseIcon className={`${iconStyles.icon} ${styles.selectedFilterRemoveIcon}`.trim()} />
            </button>
          </div>
        ))}
      </div>
    </section>
  );
}

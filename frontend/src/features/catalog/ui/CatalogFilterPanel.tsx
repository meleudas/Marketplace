import Image from "next/image";
import {
  AUTHOR_FILTER_OPTIONS,
  DEFAULT_CATALOG_MAX_PRICE,
  DEFAULT_CATALOG_MIN_PRICE,
  FORMAT_FILTER_OPTIONS,
} from "@/features/catalog/lib/catalog-filter-options";
import { toggleArrayFilter, toggleSingleFilter } from "@/features/catalog/lib/catalog-filter-utils";
import { getChildCategories } from "@/features/storefront/lib/catalog-category-filter";
import type { CatalogCategoryDto } from "@/features/storefront/model/catalog.types";
import { Button, Checkbox, CloseIcon } from "@/shared/ui";
import iconStyles from "@/shared/ui/icons/Icon.module.css";
import styles from "../screens/CatalogScreen.module.css";

const FILTER_PREVIEW_LIMIT = 4;
const CATEGORY_FILTER_PREVIEW_LIMIT = 6;

interface CatalogFilterPanelProps {
  open: boolean;
  rootCategories: CatalogCategoryDto[];
  categories: CatalogCategoryDto[];
  draftAuthors: string[];
  draftCategorySlugs: string[];
  draftFormat: string | null;
  draftMinPrice: string;
  draftMaxPrice: string;
  showAllCategories: boolean;
  showAllAuthors: boolean;
  onClose: () => void;
  onApply: () => void;
  onDraftAuthorsChange: (authors: string[] | ((current: string[]) => string[])) => void;
  onDraftRootCategoryToggle: (slug: string) => void;
  onDraftSubcategoryToggle: (rootSlug: string, subcategorySlug: string) => void;
  onDraftFormatChange: (format: string | null | ((current: string | null) => string | null)) => void;
  onDraftMinPriceChange: (value: string) => void;
  onDraftMaxPriceChange: (value: string) => void;
  onShowAllCategoriesChange: (value: boolean | ((current: boolean) => boolean)) => void;
  onShowAllAuthorsChange: (value: boolean | ((current: boolean) => boolean)) => void;
}

export function CatalogFilterPanel({
  open,
  rootCategories,
  categories,
  draftAuthors,
  draftCategorySlugs,
  draftFormat,
  draftMinPrice,
  draftMaxPrice,
  showAllCategories,
  showAllAuthors,
  onClose,
  onApply,
  onDraftAuthorsChange,
  onDraftRootCategoryToggle,
  onDraftSubcategoryToggle,
  onDraftFormatChange,
  onDraftMinPriceChange,
  onDraftMaxPriceChange,
  onShowAllCategoriesChange,
  onShowAllAuthorsChange,
}: CatalogFilterPanelProps) {
  if (!open) {
    return null;
  }

  const displayedRootCategories = showAllCategories
    ? rootCategories
    : rootCategories.slice(0, CATEGORY_FILTER_PREVIEW_LIMIT);
  const displayedAuthorOptions = showAllAuthors
    ? AUTHOR_FILTER_OPTIONS
    : AUTHOR_FILTER_OPTIONS.slice(0, FILTER_PREVIEW_LIMIT);

  const shouldShowSubcategories = (rootCategory: CatalogCategoryDto): boolean => {
    const childSlugs = getChildCategories(categories, rootCategory.id).map(
      (category) => category.slug,
    );

    return (
      draftCategorySlugs.includes(rootCategory.slug) ||
      childSlugs.some((slug) => draftCategorySlugs.includes(slug))
    );
  };

  return (
    <section className={styles.filterPanel} role="dialog" aria-modal="true" aria-label="Фільтри">
      <div className={styles.filterHeader}>
        <h2 className={styles.filterTitle}>Фільтри</h2>
        <button
          type="button"
          className={styles.filterCloseButton}
          aria-label="Закрити фільтри"
          onClick={onClose}
        >
          <CloseIcon className={iconStyles.icon} />
        </button>
      </div>

      <div className={styles.filterContent}>
        <section className={styles.filterSection} aria-labelledby="genre-filter-title">
          <h3 id="genre-filter-title" className={styles.filterSectionTitle}>
            Жанр
          </h3>
          <div className={styles.filterList}>
            {displayedRootCategories.map((category) => {
              const subcategories = getChildCategories(categories, category.id);

              return (
                <div key={category.id} className={styles.categoryFilterGroup}>
                  <Checkbox
                    label={category.name}
                    checked={draftCategorySlugs.includes(category.slug)}
                    onCheckedChange={() => onDraftRootCategoryToggle(category.slug)}
                  />

                  {shouldShowSubcategories(category) && subcategories.length > 0 ? (
                    <div className={styles.subcategoryFilterList}>
                      {subcategories.map((subcategory) => (
                        <Checkbox
                          key={subcategory.id}
                          label={subcategory.name}
                          checked={draftCategorySlugs.includes(subcategory.slug)}
                          onCheckedChange={() =>
                            onDraftSubcategoryToggle(category.slug, subcategory.slug)
                          }
                        />
                      ))}
                    </div>
                  ) : null}
                </div>
              );
            })}
          </div>
          {rootCategories.length > CATEGORY_FILTER_PREVIEW_LIMIT ? (
            <button
              type="button"
              className={styles.showAll}
              onClick={() => onShowAllCategoriesChange((value) => !value)}
            >
              {showAllCategories ? "Згорнути" : "Показати всі"}
            </button>
          ) : null}
        </section>

        <section
          className={[styles.filterSection, styles.authorSection].join(" ")}
          aria-labelledby="author-filter-title"
        >
          <div>
            <h3 id="author-filter-title" className={styles.filterSectionTitle}>
              Автор
            </h3>
            <div className={styles.filterList}>
              {displayedAuthorOptions.map((option) => (
                <Checkbox
                  key={option.value}
                  label={option.label}
                  checked={draftAuthors.includes(option.value)}
                  onCheckedChange={() =>
                    onDraftAuthorsChange((current) => toggleArrayFilter(current, option.value))
                  }
                />
              ))}
            </div>
            {AUTHOR_FILTER_OPTIONS.length > FILTER_PREVIEW_LIMIT ? (
              <button
                type="button"
                className={styles.showAll}
                onClick={() => onShowAllAuthorsChange((value) => !value)}
              >
                {showAllAuthors ? "Згорнути" : "Показати всі"}
              </button>
            ) : null}
          </div>
          <Image
            className={styles.filterCat}
            src="/filter-cat.svg"
            alt=""
            width={96}
            height={96}
            aria-hidden="true"
          />
        </section>

        <section className={styles.filterSection} aria-labelledby="format-filter-title">
          <h3 id="format-filter-title" className={styles.filterSectionTitle}>
            Формат книги
          </h3>
          <div className={styles.filterList}>
            {FORMAT_FILTER_OPTIONS.map((option) => (
              <Checkbox
                key={option.value}
                label={option.label}
                checked={draftFormat === option.value}
                onCheckedChange={() =>
                  onDraftFormatChange((current) => toggleSingleFilter(current, option.value))
                }
              />
            ))}
          </div>
        </section>

        <section className={styles.filterSection} aria-labelledby="price-filter-title">
          <h3 id="price-filter-title" className={styles.filterSectionTitle}>
            Ціна
          </h3>
          <div className={styles.priceRow}>
            <label className={styles.priceField}>
              <span className={styles.priceLabel}>Від</span>
              <input
                className={styles.priceInput}
                inputMode="numeric"
                value={draftMinPrice || DEFAULT_CATALOG_MIN_PRICE}
                onChange={(event) => onDraftMinPriceChange(event.target.value)}
              />
            </label>
            <label className={styles.priceField}>
              <span className={styles.priceLabel}>До</span>
              <input
                className={styles.priceInput}
                inputMode="numeric"
                value={draftMaxPrice || DEFAULT_CATALOG_MAX_PRICE}
                onChange={(event) => onDraftMaxPriceChange(event.target.value)}
              />
            </label>
          </div>
        </section>
      </div>

      <div className={styles.filterActions}>
        <Button
          type="button"
          variant="primary"
          size="lg"
          fullWidth
          className={styles.applyFiltersButton}
          onClick={onApply}
        >
          Застосувати
        </Button>
      </div>
    </section>
  );
}

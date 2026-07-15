"use client";

import { useState } from "react";
import {
  DEFAULT_CATALOG_MAX_PRICE,
  DEFAULT_CATALOG_MIN_PRICE,
  FORMAT_FILTER_OPTIONS,
} from "@/features/catalog/lib/catalog-filter-options";
import { getChildCategories } from "@/features/storefront/lib/catalog-category-filter";
import type { CatalogCategoryDto, CatalogFacetOptionDto } from "@/features/storefront/model/catalog.types";
import { Button, Checkbox, ChevronDownIcon } from "@/shared/ui";
import iconStyles from "@/shared/ui/icons/Icon.module.css";
import styles from "../screens/CatalogScreen.module.css";

const CATEGORY_PREVIEW_LIMIT = 6;
const AUTHOR_PREVIEW_LIMIT = 4;

type SidebarSectionKey = "category" | "author" | "format" | "price";

interface CatalogFilterSidebarProps {
  rootCategories: CatalogCategoryDto[];
  categories: CatalogCategoryDto[];
  authorOptions: CatalogFacetOptionDto[];
  authorsLoading: boolean;
  appliedCategorySlugs: string[];
  appliedAuthors: string[];
  appliedFormat: string | null;
  appliedMinPrice: string;
  appliedMaxPrice: string;
  onToggleRootCategory: (slug: string) => void;
  onToggleSubcategory: (rootSlug: string, subcategorySlug: string) => void;
  onToggleAuthor: (author: string) => void;
  onToggleFormat: (format: string) => void;
  onApplyPriceRange: (minPrice: string, maxPrice: string) => void;
  onReset: () => void;
}

function SidebarChevron({ open }: { open: boolean }) {
  return (
    <ChevronDownIcon
      className={[iconStyles.icon, styles.sidebarChevron, open ? styles.sidebarChevronOpen : ""]
        .filter(Boolean)
        .join(" ")}
    />
  );
}

export function CatalogFilterSidebar({
  rootCategories,
  categories,
  authorOptions = [],
  authorsLoading,
  appliedCategorySlugs,
  appliedAuthors,
  appliedFormat,
  appliedMinPrice,
  appliedMaxPrice,
  onToggleRootCategory,
  onToggleSubcategory,
  onToggleAuthor,
  onToggleFormat,
  onApplyPriceRange,
  onReset,
}: CatalogFilterSidebarProps) {
  const [openSections, setOpenSections] = useState<Set<SidebarSectionKey>>(
    () => new Set<SidebarSectionKey>(["category", "author", "format", "price"]),
  );
  const [showAllCategories, setShowAllCategories] = useState(false);
  const [showAllAuthors, setShowAllAuthors] = useState(false);
  const [minPriceInput, setMinPriceInput] = useState(appliedMinPrice || DEFAULT_CATALOG_MIN_PRICE);
  const [maxPriceInput, setMaxPriceInput] = useState(appliedMaxPrice || DEFAULT_CATALOG_MAX_PRICE);
  const [prevAppliedMinPrice, setPrevAppliedMinPrice] = useState(appliedMinPrice);
  const [prevAppliedMaxPrice, setPrevAppliedMaxPrice] = useState(appliedMaxPrice);

  // Keep the local price draft in sync when the applied range changes externally
  // (e.g. "Очистити"), without introducing an effect-driven render cascade.
  if (appliedMinPrice !== prevAppliedMinPrice) {
    setPrevAppliedMinPrice(appliedMinPrice);
    setMinPriceInput(appliedMinPrice || DEFAULT_CATALOG_MIN_PRICE);
  }

  if (appliedMaxPrice !== prevAppliedMaxPrice) {
    setPrevAppliedMaxPrice(appliedMaxPrice);
    setMaxPriceInput(appliedMaxPrice || DEFAULT_CATALOG_MAX_PRICE);
  }

  const toggleSection = (key: SidebarSectionKey) => {
    setOpenSections((current) => {
      const next = new Set(current);
      if (next.has(key)) {
        next.delete(key);
      } else {
        next.add(key);
      }
      return next;
    });
  };

  const hasActiveFilters =
    appliedCategorySlugs.length > 0 ||
    appliedAuthors.length > 0 ||
    Boolean(appliedFormat) ||
    Boolean(appliedMinPrice) ||
    Boolean(appliedMaxPrice);

  const displayedRootCategories = showAllCategories
    ? rootCategories
    : rootCategories.slice(0, CATEGORY_PREVIEW_LIMIT);
  const displayedAuthorOptions = showAllAuthors
    ? authorOptions
    : authorOptions.slice(0, AUTHOR_PREVIEW_LIMIT);

  const shouldShowSubcategories = (rootCategory: CatalogCategoryDto): boolean => {
    const childSlugs = getChildCategories(categories, rootCategory.id).map((child) => child.slug);

    return (
      appliedCategorySlugs.includes(rootCategory.slug) ||
      childSlugs.some((slug) => appliedCategorySlugs.includes(slug))
    );
  };

  return (
    <aside className={styles.sidebar} aria-label="Фільтри каталогу">
      <div className={styles.sidebarHeader}>
        <h2 className={styles.sidebarTitle}>Фільтри</h2>
        {hasActiveFilters ? (
          <button type="button" className={styles.sidebarReset} onClick={onReset}>
            Очистити
          </button>
        ) : null}
      </div>

      <section className={styles.sidebarSection}>
        <button
          type="button"
          className={styles.sidebarSectionHeader}
          aria-expanded={openSections.has("category")}
          onClick={() => toggleSection("category")}
        >
          <span>Жанр</span>
          <SidebarChevron open={openSections.has("category")} />
        </button>

        {openSections.has("category") ? (
          <div className={styles.sidebarSectionBody}>
            {displayedRootCategories.map((category) => {
              const subcategories = getChildCategories(categories, category.id);

              return (
                <div key={category.id} className={styles.categoryFilterGroup}>
                  <Checkbox
                    label={category.name}
                    checked={appliedCategorySlugs.includes(category.slug)}
                    onCheckedChange={() => onToggleRootCategory(category.slug)}
                  />

                  {shouldShowSubcategories(category) && subcategories.length > 0 ? (
                    <div className={styles.sidebarSubcategoryList}>
                      {subcategories.map((subcategory) => (
                        <Checkbox
                          key={subcategory.id}
                          label={subcategory.name}
                          checked={appliedCategorySlugs.includes(subcategory.slug)}
                          onCheckedChange={() => onToggleSubcategory(category.slug, subcategory.slug)}
                        />
                      ))}
                    </div>
                  ) : null}
                </div>
              );
            })}

            {rootCategories.length > CATEGORY_PREVIEW_LIMIT ? (
              <button
                type="button"
                className={styles.sidebarShowAll}
                onClick={() => setShowAllCategories((value) => !value)}
              >
                {showAllCategories ? "Згорнути" : "Показати всі"}
              </button>
            ) : null}
          </div>
        ) : null}
      </section>

      <section className={styles.sidebarSection}>
        <button
          type="button"
          className={styles.sidebarSectionHeader}
          aria-expanded={openSections.has("author")}
          onClick={() => toggleSection("author")}
        >
          <span>Автор</span>
          <SidebarChevron open={openSections.has("author")} />
        </button>

        {openSections.has("author") ? (
          <div className={styles.sidebarSectionBody}>
            {authorsLoading ? (
              <p className={styles.filterLoadingText}>Завантаження авторів...</p>
            ) : displayedAuthorOptions.length > 0 ? (
              displayedAuthorOptions.map((option) => (
                <Checkbox
                  key={option.value}
                  label={`${option.label} (${option.count})`}
                  checked={appliedAuthors.includes(option.value)}
                  onCheckedChange={() => onToggleAuthor(option.value)}
                />
              ))
            ) : (
              <p className={styles.filterLoadingText}>Авторів не знайдено</p>
            )}

            {authorOptions.length > AUTHOR_PREVIEW_LIMIT ? (
              <button
                type="button"
                className={styles.sidebarShowAll}
                onClick={() => setShowAllAuthors((value) => !value)}
              >
                {showAllAuthors ? "Згорнути" : "Показати всі"}
              </button>
            ) : null}
          </div>
        ) : null}
      </section>

      <section className={styles.sidebarSection}>
        <button
          type="button"
          className={styles.sidebarSectionHeader}
          aria-expanded={openSections.has("format")}
          onClick={() => toggleSection("format")}
        >
          <span>Формат книги</span>
          <SidebarChevron open={openSections.has("format")} />
        </button>

        {openSections.has("format") ? (
          <div className={styles.sidebarSectionBody}>
            {FORMAT_FILTER_OPTIONS.map((option) => (
              <Checkbox
                key={option.value}
                label={option.label}
                checked={appliedFormat === option.value}
                onCheckedChange={() => onToggleFormat(option.value)}
              />
            ))}
          </div>
        ) : null}
      </section>

      <section className={styles.sidebarSection}>
        <button
          type="button"
          className={styles.sidebarSectionHeader}
          aria-expanded={openSections.has("price")}
          onClick={() => toggleSection("price")}
        >
          <span>Ціна</span>
          <SidebarChevron open={openSections.has("price")} />
        </button>

        {openSections.has("price") ? (
          <div className={styles.sidebarSectionBody}>
            <div className={styles.sidebarPriceRow}>
              <label className={styles.sidebarPriceField}>
                <span className={styles.sidebarPriceLabel}>Від</span>
                <input
                  className={styles.sidebarPriceInput}
                  inputMode="numeric"
                  value={minPriceInput}
                  onChange={(event) => setMinPriceInput(event.target.value)}
                />
              </label>
              <label className={styles.sidebarPriceField}>
                <span className={styles.sidebarPriceLabel}>До</span>
                <input
                  className={styles.sidebarPriceInput}
                  inputMode="numeric"
                  value={maxPriceInput}
                  onChange={(event) => setMaxPriceInput(event.target.value)}
                />
              </label>
            </div>
            <Button
              type="button"
              variant="dark"
              size="sm"
              fullWidth
              className={styles.sidebarApplyPrice}
              onClick={() => onApplyPriceRange(minPriceInput, maxPriceInput)}
            >
              Застосувати
            </Button>
          </div>
        ) : null}
      </section>
    </aside>
  );
}

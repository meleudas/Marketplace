"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Button } from "@/shared/ui/Button";
import { Container } from "@/shared/ui/Container";
import { ChevronLeftIcon, ChevronRightIcon, CloseIcon } from "@/shared/ui/icons";
import iconStyles from "@/shared/ui/icons/Icon.module.css";
import { PageBackground } from "@/shared/ui/PageBackground";
import { Typography } from "@/shared/ui/Typography";
import styles from "./CatalogMenu.module.css";

export type CatalogFormatFilter = "all" | "електронний" | "паперовий";

export interface CatalogMenuCategory {
  id: number;
  name: string;
  slug: string;
  parentId?: number | null;
  sortOrder?: number;
}

interface CatalogMenuProps {
  open: boolean;
  categories: CatalogMenuCategory[];
  onClose: () => void;
  onCategorySelect?: (slug: string, format: CatalogFormatFilter) => void;
  onShowAll?: (format: CatalogFormatFilter) => void;
  activeFormat?: CatalogFormatFilter;
}

const FORMAT_OPTIONS: Array<{ value: CatalogFormatFilter; label: string }> = [
  { value: "all", label: "Всі" },
  { value: "електронний", label: "Електронна" },
  { value: "паперовий", label: "Паперова" },
];

function sortCategories(a: CatalogMenuCategory, b: CatalogMenuCategory): number {
  return (a.sortOrder ?? 0) - (b.sortOrder ?? 0) || a.name.localeCompare(b.name, "uk");
}

export function CatalogMenu({
  open,
  categories,
  onClose,
  onCategorySelect,
  onShowAll,
  activeFormat = "all",
}: CatalogMenuProps) {
  const [selectedFormat, setSelectedFormat] = useState<CatalogFormatFilter | null>(null);
  const [activeRootCategory, setActiveRootCategory] = useState<CatalogMenuCategory | null>(null);
  const formatFilter = selectedFormat ?? activeFormat;

  const rootCategories = useMemo(
    () => categories.filter((category) => category.parentId == null).sort(sortCategories),
    [categories],
  );

  const activeSubcategories = useMemo(() => {
    if (!activeRootCategory) {
      return [];
    }

    return categories
      .filter((category) => category.parentId === activeRootCategory.id)
      .sort(sortCategories);
  }, [activeRootCategory, categories]);

  const handleClose = useCallback(() => {
    setSelectedFormat(null);
    setActiveRootCategory(null);
    onClose();
  }, [onClose]);

  useEffect(() => {
    if (!open) {
      return;
    }

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key !== "Escape") {
        return;
      }

      if (activeRootCategory) {
        setActiveRootCategory(null);
        return;
      }

      handleClose();
    };

    window.addEventListener("keydown", handleKeyDown);

    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener("keydown", handleKeyDown);
    };
  }, [activeRootCategory, handleClose, open]);

  if (!open) {
    return null;
  }

  const handleFormatSelect = (format: CatalogFormatFilter) => {
    setSelectedFormat(format);
  };

  const handleRootCategoryClick = (category: CatalogMenuCategory) => {
    const hasSubcategories = categories.some((item) => item.parentId === category.id);
    if (hasSubcategories) {
      setActiveRootCategory(category);
      return;
    }

    onCategorySelect?.(category.slug, formatFilter);
    handleClose();
  };

  const handleSubcategoryClick = (slug: string) => {
    onCategorySelect?.(slug, formatFilter);
    handleClose();
  };

  const handleShowSection = () => {
    if (!activeRootCategory) {
      return;
    }

    onCategorySelect?.(activeRootCategory.slug, formatFilter);
    handleClose();
  };

  const handleShowAll = () => {
    onShowAll?.(formatFilter);
    handleClose();
  };

  const handleBackToCatalog = () => {
    setActiveRootCategory(null);
  };

  if (activeRootCategory) {
    return (
      <div
        className={styles.overlay}
        role="dialog"
        aria-modal="true"
        aria-labelledby="catalog-menu-title"
      >
        <PageBackground />

        <Container className={styles.content}>
          <header className={styles.header}>
            <h1 id="catalog-menu-title">
              <Typography variant="h2" as="span">
                {activeRootCategory.name}
              </Typography>
            </h1>
            <button type="button" className={styles.closeButton} aria-label="Закрити" onClick={handleClose}>
              <CloseIcon className={iconStyles.icon} />
            </button>
          </header>

          <button type="button" className={styles.backButton} onClick={handleBackToCatalog}>
            <ChevronLeftIcon className={`${iconStyles.icon} ${styles.backButtonIcon}`.trim()} />
            <span>До каталогу</span>
          </button>

          <nav className={styles.categoryList} aria-label="Підкатегорії">
            {activeSubcategories.map((category) => (
              <button
                key={category.id}
                type="button"
                className={styles.subcategoryItem}
                onClick={() => handleSubcategoryClick(category.slug)}
              >
                {category.name}
              </button>
            ))}
          </nav>

          <Button
            type="button"
            variant="primary"
            size="lg"
            fullWidth
            className={styles.showAllButton}
            onClick={handleShowSection}
          >
            Усе в розділі
          </Button>
        </Container>
      </div>
    );
  }

  return (
    <div
      className={styles.overlay}
      role="dialog"
      aria-modal="true"
      aria-labelledby="catalog-menu-title"
    >
      <PageBackground />

      <Container className={styles.content}>
        <header className={styles.header}>
          <h1 id="catalog-menu-title">
            <Typography variant="h2" as="span">
              Каталог
            </Typography>
          </h1>
          <button type="button" className={styles.closeButton} aria-label="Закрити" onClick={handleClose}>
            <CloseIcon className={iconStyles.icon} />
          </button>
        </header>

        <div className={styles.formatRow} role="tablist" aria-label="Формат книг">
          {FORMAT_OPTIONS.map((option) => (
            <Button
              key={option.value}
              type="button"
              role="tab"
              variant="filter"
              size="sm"
              selectable
              selected={formatFilter === option.value}
              aria-selected={formatFilter === option.value}
              className={styles.formatButton}
              onClick={() => handleFormatSelect(option.value)}
            >
              {option.label}
            </Button>
          ))}
        </div>

        <nav className={styles.categoryList} aria-label="Категорії">
          {rootCategories.map((category) => (
            <button
              key={category.id}
              type="button"
              className={styles.categoryItem}
              onClick={() => handleRootCategoryClick(category)}
            >
              <span>{category.name}</span>
              <ChevronRightIcon className={`${iconStyles.icon} ${styles.categoryItemIcon}`.trim()} />
            </button>
          ))}
        </nav>

        <Button
          type="button"
          variant="primary"
          size="lg"
          fullWidth
          className={styles.showAllButton}
          onClick={handleShowAll}
        >
          Показати все
        </Button>
      </Container>
    </div>
  );
}

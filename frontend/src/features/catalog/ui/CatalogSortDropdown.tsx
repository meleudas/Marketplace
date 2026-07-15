"use client";

import { useEffect, useRef, useState } from "react";
import {
  CATALOG_PRODUCT_SORT_OPTIONS,
  getCatalogProductSortLabel,
  type CatalogProductSort,
} from "@/features/storefront/lib/catalog-product-sort";
import { ChevronDownIcon } from "@/shared/ui";
import iconStyles from "@/shared/ui/icons/Icon.module.css";
import styles from "../screens/CatalogScreen.module.css";

interface CatalogSortDropdownProps {
  selectedSort: CatalogProductSort;
  onSelect: (sort: CatalogProductSort) => void;
}

export function CatalogSortDropdown({ selectedSort, onSelect }: CatalogSortDropdownProps) {
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) {
      return;
    }

    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    };

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setOpen(false);
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    window.addEventListener("keydown", handleKeyDown);

    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
      window.removeEventListener("keydown", handleKeyDown);
    };
  }, [open]);

  return (
    <div className={styles.sortDropdown} ref={containerRef}>
      <button
        type="button"
        className={styles.sortDropdownTrigger}
        aria-haspopup="listbox"
        aria-expanded={open}
        onClick={() => setOpen((value) => !value)}
      >
        <span>{getCatalogProductSortLabel(selectedSort)}</span>
        <ChevronDownIcon
          className={[iconStyles.icon, open ? styles.sortDropdownChevronOpen : ""]
            .filter(Boolean)
            .join(" ")}
        />
      </button>

      {open ? (
        <ul className={styles.sortDropdownPanel} role="listbox">
          {CATALOG_PRODUCT_SORT_OPTIONS.map((option) => {
            const isActive = option.value === selectedSort;

            return (
              <li key={option.value} role="option" aria-selected={isActive}>
                <button
                  type="button"
                  className={isActive ? styles.sortDropdownOptionActive : styles.sortDropdownOption}
                  onClick={() => {
                    onSelect(option.value);
                    setOpen(false);
                  }}
                >
                  {option.label}
                </button>
              </li>
            );
          })}
        </ul>
      ) : null}
    </div>
  );
}

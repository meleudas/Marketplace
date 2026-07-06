"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { searchCatalogProducts, type CatalogSearchProductDto } from "@/shared/api/catalog-search.api";
import { Container } from "./Container";
import { Spinner } from "./Spinner";
import { TextField } from "./TextField";
import {
  BookTopLogo,
  CartIcon,
  ChevronRightIcon,
  CloseIcon,
  FooterCatIllustration,
  MenuIcon,
  MicrophoneIcon,
  SearchIcon,
  UserIcon,
} from "./icons";
import iconStyles from "./icons/Icon.module.css";
import styles from "./Header.module.css";

interface SearchPreviewItem {
  id: string;
  slug: string;
  title: string;
  price: number | string;
}

interface HeaderProps {
  homeHref?: string;
  userHref?: string;
  cartHref?: string;
  searchPlaceholder?: string;
  onSearchQueryChange?: (value: string) => void;
  onMenuClick?: () => void;
}

export function Header({
  homeHref = "/",
  userHref = "/auth",
  cartHref = "#",
  searchPlaceholder = "Пошук",
  onSearchQueryChange,
  onMenuClick,
}: HeaderProps) {
  const [isSearchExpanded, setIsSearchExpanded] = useState(false);
  const [searchValue, setSearchValue] = useState("");
  const [searchResults, setSearchResults] = useState<SearchPreviewItem[]>([]);
  const [searchLoading, setSearchLoading] = useState(false);
  const hasSearchQuery = Boolean(searchValue.trim());

  const handleOpenSearch = () => {
    setIsSearchExpanded(true);
  };

  const handleCloseSearch = () => {
    setIsSearchExpanded(false);
    setSearchValue("");
    setSearchResults([]);
    onSearchQueryChange?.("");
  };

  const previewItems = useMemo(() => searchResults.slice(0, 4), [searchResults]);

  useEffect(() => {
    if (!isSearchExpanded) {
      return;
    }

    const previousBodyOverflow = document.body.style.overflow;
    const previousHtmlOverflow = document.documentElement.style.overflow;

    document.body.style.overflow = "hidden";
    document.documentElement.style.overflow = "hidden";

    return () => {
      document.body.style.overflow = previousBodyOverflow;
      document.documentElement.style.overflow = previousHtmlOverflow;
    };
  }, [isSearchExpanded]);

  useEffect(() => {
    onSearchQueryChange?.(searchValue);
  }, [onSearchQueryChange, searchValue]);

  useEffect(() => {
    const query = searchValue.trim();

    if (!isSearchExpanded || query.length === 0) {
      setSearchResults([]);
      setSearchLoading(false);
      return;
    }

    let cancelled = false;
    const timeoutId = window.setTimeout(() => {
      const load = async () => {
        try {
          setSearchLoading(true);
          const result = await searchCatalogProducts({ query, limit: 4 });

          if (cancelled) {
            return;
          }

          setSearchResults(
            result.items.map((item: CatalogSearchProductDto) => ({
              id: String(item.id),
              slug: item.slug,
              title: item.name,
              price: item.price,
            })),
          );
        } catch {
          if (!cancelled) {
            setSearchResults([]);
          }
        } finally {
          if (!cancelled) {
            setSearchLoading(false);
          }
        }
      };

      void load();
    }, 300);

    return () => {
      cancelled = true;
      window.clearTimeout(timeoutId);
    };
  }, [isSearchExpanded, searchValue]);

  return (
    <header className={styles.header} data-search-open={isSearchExpanded ? "true" : "false"}>
      <Container className={styles.inner}>
        <div className={styles.topRow}>
          <button
            type="button"
            className={styles.iconAction}
            aria-label="Відкрити меню"
            onClick={onMenuClick}
          >
            <MenuIcon className={iconStyles.icon} />
          </button>

          <Link href={homeHref} className={styles.logo} aria-label="BOOK TOP — на головну">
            <BookTopLogo className={styles.logoImage} />
          </Link>

          <div className={styles.actions}>
            <Link href={userHref} className={styles.iconAction} aria-label="Профіль">
              <UserIcon className={iconStyles.icon} />
            </Link>
            <Link href={cartHref} className={styles.iconAction} aria-label="Кошик">
              <CartIcon className={iconStyles.icon} />
            </Link>
          </div>
        </div>

        <TextField
          kind="search"
          className={`${styles.search} ${styles.primarySearch}`}
          value={searchValue}
          placeholder={searchPlaceholder}
          aria-label="Пошук"
          leadingIcon={<SearchIcon className={iconStyles.icon} />}
          trailingIcon={<MicrophoneIcon className={iconStyles.icon} />}
          onFocus={handleOpenSearch}
          onChange={(event) => setSearchValue(event.target.value)}
        />

        <div className={styles.searchSheet}>
          <div className={styles.searchSheetInner}>
            <div className={styles.searchSheetHeader}>
              <h2 className={styles.searchTitle}>Пошук</h2>
              <button
                type="button"
                className={styles.closeButton}
                aria-label="Закрити пошук"
                onClick={handleCloseSearch}
              >
                <CloseIcon className={iconStyles.icon} />
              </button>
            </div>

            <TextField
              kind="search"
              className={`${styles.search} ${styles.sheetSearchField}`}
              value={searchValue}
              placeholder={searchPlaceholder}
              aria-label="Пошук"
              leadingIcon={<SearchIcon className={iconStyles.icon} />}
              trailingIcon={<MicrophoneIcon className={iconStyles.icon} />}
              autoFocus
              onChange={(event) => setSearchValue(event.target.value)}
            />

            {hasSearchQuery ? (
              <section className={styles.previewCard} aria-label="Підбірка для вас">
                <div className={styles.previewHeader}>
                  <span className={styles.previewTitle}>Для вас</span>
                  <ChevronRightIcon className={iconStyles.icon} />
                </div>

                {searchLoading ? (
                  <div className={styles.loadingState} role="status" aria-live="polite">
                    <Spinner aria-hidden="true" />
                    <span className={styles.loadingText}>Завантаження...</span>
                  </div>
                ) : previewItems.length > 0 ? (
                  <ul className={styles.previewList}>
                    {previewItems.map((item) => (
                      <li key={item.id}>
                        <Link
                          href={`/products/${item.slug}`}
                          className={styles.previewItem}
                          onClick={handleCloseSearch}
                        >
                          <span className={styles.previewName}>{item.title}</span>
                          <span className={styles.previewPrice}>
                            {typeof item.price === "number"
                              ? item.price.toLocaleString("uk-UA")
                              : item.price}{" "}
                            грн
                          </span>
                        </Link>
                      </li>
                    ))}
                  </ul>
                ) : (
                  <p className={styles.emptyState}>Нічого не знайдено</p>
                )}
              </section>
            ) : null}
          </div>

          <FooterCatIllustration className={styles.cat} />
        </div>
      </Container>
    </header>
  );
}

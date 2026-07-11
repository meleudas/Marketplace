"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  searchCatalogProducts,
  type CatalogSearchProductDto,
} from "@/shared/api/catalog-search.api";
import { useCartStore } from "@/features/cart/model/cart.store";
import { useAuth } from "@/features/auth/model/auth.store";
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
  searchValue?: string;
  searchPlaceholder?: string;
  onSearchChange?: (value: string) => void;
  onMenuClick?: () => void;
}

const DESKTOP_MEDIA_QUERY = "(min-width: 1024px)";

export function Header({
  homeHref = "/",
  userHref = "/auth",
  cartHref = "/cart",
  searchValue: controlledSearchValue,
  searchPlaceholder = "Пошук",
  onSearchChange,
  onMenuClick,
}: HeaderProps) {
  const pathname = usePathname();
  const isCatalogRoute = pathname === "/catalog" || pathname.startsWith("/catalog/");
  const searchWrapRef = useRef<HTMLDivElement>(null);

  const [isDesktop, setIsDesktop] = useState(false);
  const [isSearchExpanded, setIsSearchExpanded] = useState(false);
  const [isSearchDropdownOpen, setIsSearchDropdownOpen] = useState(false);
  const [searchValue, setSearchValue] = useState(controlledSearchValue ?? "");
  const [searchResults, setSearchResults] = useState<SearchPreviewItem[]>([]);
  const [searchLoading, setSearchLoading] = useState(false);

  const { isAuthenticated, initialized: authInitialized } = useAuth();
  const {
    totalItems: cartCount,
    loadCart,
    initialized: cartInitialized,
  } = useCartStore();

  useEffect(() => {
    const mediaQuery = window.matchMedia(DESKTOP_MEDIA_QUERY);
    const updateViewport = () => setIsDesktop(mediaQuery.matches);

    updateViewport();
    mediaQuery.addEventListener("change", updateViewport);

    return () => mediaQuery.removeEventListener("change", updateViewport);
  }, []);

  useEffect(() => {
    if (authInitialized && isAuthenticated && !cartInitialized) {
      loadCart();
    }
  }, [authInitialized, isAuthenticated, cartInitialized, loadCart]);

  const hasSearchQuery = Boolean(searchValue.trim());
  const isMobileSearchOpen = !isDesktop && isSearchExpanded;
  const isSearchActive = isDesktop ? isSearchDropdownOpen : isSearchExpanded;
  const showSearchPreview = hasSearchQuery && isSearchActive;

  const handleOpenSearch = () => {
    if (isDesktop) {
      setIsSearchDropdownOpen(true);
      return;
    }

    setIsSearchExpanded(true);
  };

  const handleCloseSearch = () => {
    setIsSearchExpanded(false);
    setIsSearchDropdownOpen(false);
    setSearchValue("");
    setSearchResults([]);
    onSearchChange?.("");
  };

  const handleCloseSearchResults = () => {
    setIsSearchExpanded(false);
    setIsSearchDropdownOpen(false);
    setSearchResults([]);
  };

  const previewItems = useMemo(
    () => searchResults.slice(0, 4),
    [searchResults],
  );

  useEffect(() => {
    if (!isMobileSearchOpen) {
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
  }, [isMobileSearchOpen]);

  useEffect(() => {
    onSearchChange?.(searchValue);
  }, [onSearchChange, searchValue]);

  useEffect(() => {
    const query = searchValue.trim();

    if (!isSearchActive || query.length === 0) {
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
  }, [isSearchActive, searchValue]);

  useEffect(() => {
    if (!isDesktop || !isSearchDropdownOpen) {
      return;
    }

    const handlePointerDown = (event: MouseEvent) => {
      if (!searchWrapRef.current?.contains(event.target as Node)) {
        setIsSearchDropdownOpen(false);
      }
    };

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setIsSearchDropdownOpen(false);
      }
    };

    document.addEventListener("mousedown", handlePointerDown);
    document.addEventListener("keydown", handleKeyDown);

    return () => {
      document.removeEventListener("mousedown", handlePointerDown);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [isDesktop, isSearchDropdownOpen]);

  useEffect(() => {
    if (!isDesktop) {
      setIsSearchDropdownOpen(false);
    } else {
      setIsSearchExpanded(false);
    }
  }, [isDesktop]);

  const searchPreview = showSearchPreview ? (
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
                onClick={handleCloseSearchResults}
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
  ) : null;

  return (
    <header
      className={styles.header}
      data-search-open={isMobileSearchOpen ? "true" : "false"}
      data-search-dropdown-open={isDesktop && isSearchDropdownOpen ? "true" : "false"}
    >
      <Container className={styles.inner}>
        <div className={styles.topRow}>
          {onMenuClick ? (
            <button
              type="button"
              className={`${styles.iconAction} ${styles.menuBtn}`}
              aria-label="Відкрити каталог"
              onClick={onMenuClick}
            >
              <MenuIcon className={iconStyles.icon} />
            </button>
          ) : null}

          <div className={styles.brandGroup}>
            <Link
              href={homeHref}
              className={styles.logo}
              aria-label="BOOK TOP — на головну"
            >
              <BookTopLogo className={styles.logoImage} />
            </Link>

            {onMenuClick ? (
              <nav className={styles.nav} aria-label="Головна навігація">
                <button
                  type="button"
                  className={`${styles.navLink} ${isCatalogRoute ? styles.navLinkActive : ""}`.trim()}
                  aria-current={isCatalogRoute ? "page" : undefined}
                  onClick={onMenuClick}
                >
                  Каталог
                </button>
              </nav>
            ) : null}
          </div>

          <div className={styles.searchWrap} ref={searchWrapRef}>
            <TextField
              kind="search"
              className={`${styles.search} ${styles.primarySearch}`}
              value={searchValue}
              placeholder={searchPlaceholder}
              aria-label="Пошук"
              aria-expanded={isDesktop ? isSearchDropdownOpen : undefined}
              aria-controls={isDesktop ? "header-search-dropdown" : undefined}
              leadingIcon={<SearchIcon className={iconStyles.icon} />}
              trailingIcon={<MicrophoneIcon className={iconStyles.icon} />}
              onFocus={handleOpenSearch}
              onChange={(event) => {
                setSearchValue(event.target.value);
                if (isDesktop) {
                  setIsSearchDropdownOpen(true);
                }
              }}
            />

            {isDesktop && showSearchPreview ? (
              <div
                id="header-search-dropdown"
                className={styles.searchDropdown}
                role="listbox"
                aria-label="Результати пошуку"
              >
                {searchPreview}
              </div>
            ) : null}
          </div>

          <div className={styles.actions}>
            <Link
              href={userHref}
              className={styles.iconAction}
              aria-label="Профіль"
            >
              <UserIcon className={iconStyles.icon} />
            </Link>
            <Link
              href={cartHref}
              className={`${styles.iconAction} ${styles.cartIconWrap}`}
              aria-label="Кошик"
            >
              <CartIcon className={iconStyles.icon} />
              {cartCount > 0 && (
                <span className={styles.cartBadge}>
                  {cartCount > 99 ? "99+" : cartCount}
                </span>
              )}
            </Link>
          </div>
        </div>

        <div className={styles.searchSheet} aria-hidden={!isMobileSearchOpen}>
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
              autoFocus={isMobileSearchOpen}
              onChange={(event) => setSearchValue(event.target.value)}
            />

            {searchPreview}
          </div>

          <FooterCatIllustration className={styles.cat} />
        </div>
      </Container>
    </header>
  );
}

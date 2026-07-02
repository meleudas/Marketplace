import Link from "next/link";
import { Container } from "./Container";
import { TextField } from "./TextField";
import { BookTopLogo, CartIcon, MenuIcon, MicrophoneIcon, SearchIcon, UserIcon } from "./icons";
import iconStyles from "./icons/Icon.module.css";
import styles from "./Header.module.css";

interface HeaderProps {
  homeHref?: string;
  userHref?: string;
  cartHref?: string;
  searchPlaceholder?: string;
  onMenuClick?: () => void;
}

export function Header({
  homeHref = "/",
  userHref = "/auth",
  cartHref = "#",
  searchPlaceholder = "Пошук",
  onMenuClick,
}: HeaderProps) {
  return (
    <header className={styles.header}>
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
          className={styles.search}
          placeholder={searchPlaceholder}
          aria-label="Пошук"
          leadingIcon={<SearchIcon className={iconStyles.icon} />}
          trailingIcon={<MicrophoneIcon className={iconStyles.icon} />}
        />
      </Container>
    </header>
  );
}

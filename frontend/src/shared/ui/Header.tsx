import { Container } from "./Container";
import { Button } from "./Button";
import { IconButton } from "./IconButton";
import { TextField } from "./TextField";
import styles from "./Header.module.css";

interface NavItem {
  label: string;
  href: string;
}

interface HeaderProps {
  brand?: string;
  navItems?: NavItem[];
}

const DEFAULT_NAV: NavItem[] = [
  { label: "Каталог", href: "#" },
  { label: "Категорії", href: "#" },
  { label: "Про нас", href: "#" },
  { label: "Контакти", href: "#" },
];

export function Header({ brand = "Marketplace", navItems = DEFAULT_NAV }: HeaderProps) {
  return (
    <header className={styles.header}>
      <Container className={styles.inner}>
        <span className={styles.brand}>{brand}</span>

        <nav className={styles.nav} aria-label="Головна навігація">
          {navItems.map((item) => (
            <a key={item.label} href={item.href} className={styles.link}>
              {item.label}
            </a>
          ))}
        </nav>

        <div className={styles.search}>
          <TextField
            type="search"
            placeholder="Пошук товарів..."
            aria-label="Пошук товарів"
            leadingIcon={<span>⌕</span>}
          />
        </div>

        <div className={styles.actions}>
          <IconButton label="Обране" icon={<span>♡</span>} />
          <IconButton label="Кошик" icon={<span>🛒</span>} />
          <Button variant="primary" size="sm">
            Увійти
          </Button>
        </div>
      </Container>
    </header>
  );
}

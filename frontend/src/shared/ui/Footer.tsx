import { Container } from "./Container";
import styles from "./Footer.module.css";

interface FooterColumn {
  title: string;
  links: string[];
}

interface FooterProps {
  brand?: string;
  columns?: FooterColumn[];
}

const DEFAULT_COLUMNS: FooterColumn[] = [
  { title: "Покупцям", links: ["Як замовити", "Оплата", "Доставка", "Повернення"] },
  { title: "Продавцям", links: ["Стати продавцем", "Тарифи", "Правила"] },
  { title: "Компанія", links: ["Про нас", "Вакансії", "Блог", "Контакти"] },
];

export function Footer({ brand = "Marketplace", columns = DEFAULT_COLUMNS }: FooterProps) {
  return (
    <footer className={styles.footer}>
      <Container className={styles.inner}>
        <div className={styles.about}>
          <span className={styles.brand}>{brand}</span>
          <p className={styles.tagline}>Маркетплейс для покупців і продавців.</p>
        </div>

        <div className={styles.columns}>
          {columns.map((column) => (
            <div key={column.title} className={styles.column}>
              <h4 className={styles.columnTitle}>{column.title}</h4>
              <ul className={styles.list}>
                {column.links.map((link) => (
                  <li key={link}>
                    <a href="#" className={styles.link}>
                      {link}
                    </a>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      </Container>

      <Container className={styles.bottom}>
        <span>© {new Date().getFullYear()} {brand}. Усі права захищено.</span>
      </Container>
    </footer>
  );
}

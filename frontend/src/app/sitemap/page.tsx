import type { Metadata } from "next";
import Link from "next/link";
import { PageLayout } from "@/shared/ui";
import styles from "./page.module.css";

export const metadata: Metadata = {
  title: "Карта сайту | Booktop",
  description: "Карта сайту Booktop — швидка навігація по всіх сторінках інтернет-магазину.",
};

export default function SitemapPage() {
  return (
    <PageLayout headerProps={{}} footerProps={{}}>
      <div className={styles.page}>
        <h1 className={styles.title}>Карта сайту</h1>

        <nav className={styles.sitemapNav} aria-label="Карта сайту">
          <div className={styles.sitemapGroup}>
            <h2 className={styles.sitemapGroupTitle}>Каталог</h2>
            <ul className={styles.sitemapList}>
              <li><Link href="/catalog" className={styles.sitemapLink}>Усі книги</Link></li>
              <li><Link href="/catalog?sort=popular" className={styles.sitemapLink}>Популярні</Link></li>
              <li><Link href="/catalog?sort=newest" className={styles.sitemapLink}>Новинки</Link></li>
              <li><Link href="/catalog?sort=sale" className={styles.sitemapLink}>Акції</Link></li>
              <li><Link href="/catalog?format=електронний" className={styles.sitemapLink}>Електронні книги</Link></li>
              <li><Link href="/catalog?format=аудіо" className={styles.sitemapLink}>Аудіокниги</Link></li>
              <li><Link href="/catalog?age=діти" className={styles.sitemapLink}>Дитячі книги</Link></li>
              <li><Link href="/catalog?age=підлітки" className={styles.sitemapLink}>Книги для підлітків</Link></li>
              <li><Link href="/bestsellers" className={styles.sitemapLink}>Бестселери</Link></li>
            </ul>
          </div>

          <div className={styles.sitemapGroup}>
            <h2 className={styles.sitemapGroupTitle}>Жанри</h2>
            <ul className={styles.sitemapList}>
              <li><Link href="/catalog?genre=художня" className={styles.sitemapLink}>Художня література</Link></li>
              <li><Link href="/catalog?genre=нон-фікшн" className={styles.sitemapLink}>Нон-фікшн</Link></li>
              <li><Link href="/catalog?genre=бізнес" className={styles.sitemapLink}>Бізнес та економіка</Link></li>
              <li><Link href="/catalog?genre=психологія" className={styles.sitemapLink}>Психологія</Link></li>
              <li><Link href="/catalog?genre=історія" className={styles.sitemapLink}>Історія</Link></li>
              <li><Link href="/catalog?genre=наука" className={styles.sitemapLink}>Наука та технології</Link></li>
              <li><Link href="/catalog?genre=поезія" className={styles.sitemapLink}>Поезія</Link></li>
              <li><Link href="/catalog?genre=комікси" className={styles.sitemapLink}>Комікси та графічні новели</Link></li>
            </ul>
          </div>

          <div className={styles.sitemapGroup}>
            <h2 className={styles.sitemapGroupTitle}>Сервіси</h2>
            <ul className={styles.sitemapList}>
              <li><Link href="/delivery-payment" className={styles.sitemapLink}>Доставка і оплата</Link></li>
              <li><Link href="/returns" className={styles.sitemapLink}>Повернення товару</Link></li>
              <li><Link href="/feedback" className={styles.sitemapLink}>Зворотний зв&apos;язок</Link></li>
              <li><Link href="/resales" className={styles.sitemapLink}>Перепродажі</Link></li>
              <li><Link href="/loyalty" className={styles.sitemapLink}>Програма лояльності</Link></li>
              <li><Link href="/gift-certificates" className={styles.sitemapLink}>Подарункові сертифікати</Link></li>
            </ul>
          </div>

          <div className={styles.sitemapGroup}>
            <h2 className={styles.sitemapGroupTitle}>Акаунт</h2>
            <ul className={styles.sitemapList}>
              <li><Link href="/login" className={styles.sitemapLink}>Вхід</Link></li>
              <li><Link href="/register" className={styles.sitemapLink}>Реєстрація</Link></li>
              <li><Link href="/account" className={styles.sitemapLink}>Мій акаунт</Link></li>
              <li><Link href="/account/orders" className={styles.sitemapLink}>Мої замовлення</Link></li>
              <li><Link href="/account/favorites" className={styles.sitemapLink}>Обране</Link></li>
              <li><Link href="/account/reviews" className={styles.sitemapLink}>Мої відгуки</Link></li>
              <li><Link href="/account/resales" className={styles.sitemapLink}>Мої перепродажі</Link></li>
              <li><Link href="/account/loyalty" className={styles.sitemapLink}>Бали лояльності</Link></li>
            </ul>
          </div>

          <div className={styles.sitemapGroup}>
            <h2 className={styles.sitemapGroupTitle}>Про Booktop</h2>
            <ul className={styles.sitemapList}>
              <li><Link href="/about" className={styles.sitemapLink}>Про нас</Link></li>
              <li><Link href="/projects" className={styles.sitemapLink}>Проєкти</Link></li>
              <li><Link href="/events" className={styles.sitemapLink}>Події</Link></li>
              <li><Link href="/partners" className={styles.sitemapLink}>Партнери</Link></li>
              <li><Link href="/blog" className={styles.sitemapLink}>Блог</Link></li>
              <li><Link href="/careers" className={styles.sitemapLink}>Вакансії</Link></li>
              <li><Link href="/contacts" className={styles.sitemapLink}>Контакти</Link></li>
            </ul>
          </div>

          <div className={styles.sitemapGroup}>
            <h2 className={styles.sitemapGroupTitle}>Допомога</h2>
            <ul className={styles.sitemapList}>
              <li><Link href="/faq" className={styles.sitemapLink}>Часті питання</Link></li>
              <li><Link href="/contacts" className={styles.sitemapLink}>Зв&apos;язатися з нами</Link></li>
              <li><Link href="/delivery-payment" className={styles.sitemapLink}>Як замовити</Link></li>
              <li><Link href="/returns" className={styles.sitemapLink}>Повернення та обмін</Link></li>
              <li><Link href="/sitemap" className={styles.sitemapLink}>Карта сайту</Link></li>
            </ul>
          </div>

          <div className={styles.sitemapGroup}>
            <h2 className={styles.sitemapGroupTitle}>Акції та пропозиції</h2>
            <ul className={styles.sitemapList}>
              <li><Link href="/sales" className={styles.sitemapLink}>Акції</Link></li>
              <li><Link href="/catalog?sort=sale" className={styles.sitemapLink}>Товари зі знижкою</Link></li>
              <li><Link href="/gift-certificates" className={styles.sitemapLink}>Подарункові сертифікати</Link></li>
              <li><Link href="/loyalty" className={styles.sitemapLink}>Програма лояльності</Link></li>
            </ul>
          </div>

          <div className={styles.sitemapGroup}>
            <h2 className={styles.sitemapGroupTitle}>Правова інформація</h2>
            <ul className={styles.sitemapList}>
              <li><Link href="/terms" className={styles.sitemapLink}>Умови використання</Link></li>
              <li><Link href="/offer" className={styles.sitemapLink}>Публічний договір (оферта)</Link></li>
              <li><Link href="/privacy" className={styles.sitemapLink}>Політика конфіденційності</Link></li>
              <li><Link href="/cookie-policy" className={styles.sitemapLink}>Політика файлів cookie</Link></li>
              <li><Link href="/processing-data" className={styles.sitemapLink}>Обробка персональних даних</Link></li>
            </ul>
          </div>
        </nav>
      </div>
    </PageLayout>
  );
}

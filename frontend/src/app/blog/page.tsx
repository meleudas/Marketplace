import type { Metadata } from "next";
import Link from "next/link";
import { PageLayout } from "@/shared/ui";
import styles from "./page.module.css";

export const metadata: Metadata = {
  title: "Блог | Booktop",
  description:
    "Блог Booktop — новини книжкового світу, огляди, інтерв'ю з авторами, літературні дайджести.",
};

/* ── Mock data ─────────────────────────────────────────────── */

interface BlogPost {
  slug: string;
  title: string;
  excerpt: string;
  date: string;
  author: string;
  category: string;
  readTime: string;
  featured?: boolean;
}

const ALL_POSTS: BlogPost[] = [
  {
    slug: "oglyad-knygy-proklin-kapitana",
    title: 'Огляд книги «Прокляття капітана» — епічне фентезі, яке зачаровує з перших сторінок',
    excerpt:
      "Новий роман українського автора Олексія Щербини — це море пригод, магічні світи та неймовірні персонажі, які не відпускають до останньої сторінки.",
    date: "12 липня 2026",
    author: "Марія Коваленко",
    category: "Огляди",
    readTime: "8 хв читання",
    featured: true,
  },
  {
    slug: "interviu-z-avtorom",
    title:
      "Інтерв'ю з Андрієм Содоморою: про творчість, натхнення та майбутнє української літератури",
    excerpt:
      "Відомий український письменник розповідає про свій новий роман, процес написання та чому українська література переживає золоту добу.",
    date: "8 липня 2026",
    author: "Дмитро Ткаченко",
    category: "Інтерв'ю",
    readTime: "12 хв читання",
  },
  {
    slug: "litatura-digest-2026",
    title:
      "Літературний дайджест: найважливіші книжкові події червня 2026",
    excerpt:
      "Книжкові фестивалі, нові видання, переклади та літературні премії — все, що відбувалося у світі української книги минулого місяця.",
    date: "2 липня 2026",
    author: "Олена Петренко",
    category: "Дайджести",
    readTime: "6 хв читання",
  },
  {
    slug: "top-10-knyg-vesny-2026",
    title:
      "Топ-10 книг весни 2026: що варто прочитати прямо зараз",
    excerpt:
      "Зібрали найкращі видання весни — від захопливих трилерів до глибоких документальних оповідей. Ці книги точно заслуговують вашої уваги.",
    date: "25 червня 2026",
    author: "Марія Коваленко",
    category: "Новини",
    readTime: "5 хв читання",
  },
  {
    slug: "ukrainska-proza-2026",
    title:
      "Українська проза 2026: п'ять дебютних романів, які вражають",
    excerpt:
      "Молоді автори стрімко змінюють літературний ландшафт. Ці перші романи заслуговують на увагу кожного читача.",
    date: "18 червня 2026",
    author: "Дмитро Ткаченко",
    category: "Огляди",
    readTime: "7 хв читання",
  },
  {
    slug: "knizhkova-kraina-dityachi-knigi",
    title:
      "Дитячі книжки, які варто подарувати: добірка для юних читачів",
    excerpt:
      "Від картинок для найменших до захопливих пригод для підлітків — наші рекомендації для дитячого читання.",
    date: "10 червня 2026",
    author: "Олена Петренко",
    category: "Дайджести",
    readTime: "4 хв читання",
  },
];

const CATEGORIES = ["Всі", "Огляди", "Інтерв'ю", "Дайджести", "Новини"] as const;

/* ── Page component ────────────────────────────────────────── */

export default function BlogPage() {
  const featured = ALL_POSTS.find((p) => p.featured) ?? ALL_POSTS[0];
  const rest = ALL_POSTS.filter((p) => p.slug !== featured.slug);

  return (
    <PageLayout headerProps={{}} footerProps={{}}>
      <div className={styles.page}>
        {/* ── Hero ──────────────────────────────────────────── */}
        <section className={styles.hero}>
          <span className={styles.heroBadge}>Блог Booktop</span>
          <h1 className={styles.heroTitle}>
            Книжковий світ
            <br />
            <span className={styles.heroAccent}>в кожній статті</span>
          </h1>
          <p className={styles.heroSub}>
            Огляди, інтерв&apos;ю, дайджести та рекомендації від наших
            редакторів. Знайдіть свою наступну улюблену книгу.
          </p>
        </section>

        {/* ── Category tabs ─────────────────────────────────── */}
        <nav className={styles.tabs} aria-label="Фільтр за категоріями">
          {CATEGORIES.map((cat) => (
            <span key={cat} className={styles.tab}>
              {cat}
            </span>
          ))}
        </nav>

        {/* ── Featured post ─────────────────────────────────── */}
        <Link
          href={`/blog/${featured.slug}`}
          className={styles.featuredCard}
        >
          <div className={styles.featuredImage}>
            <span className={styles.featuredImagePlaceholder}>📖</span>
          </div>
          <div className={styles.featuredBody}>
            <span className={styles.categoryBadge}>{featured.category}</span>
            <h2 className={styles.featuredTitle}>{featured.title}</h2>
            <p className={styles.featuredExcerpt}>{featured.excerpt}</p>
            <div className={styles.meta}>
              <span>{featured.author}</span>
              <span className={styles.metaDot}>·</span>
              <span>{featured.date}</span>
              <span className={styles.metaDot}>·</span>
              <span>{featured.readTime}</span>
            </div>
          </div>
        </Link>

        {/* ── Post grid ─────────────────────────────────────── */}
        <div className={styles.grid}>
          {rest.map((post) => (
            <Link
              key={post.slug}
              href={`/blog/${post.slug}`}
              className={styles.card}
            >
              <div className={styles.cardImage}>
                <span className={styles.cardImagePlaceholder}>📚</span>
              </div>
              <div className={styles.cardBody}>
                <span className={styles.categoryBadge}>{post.category}</span>
                <h3 className={styles.cardTitle}>{post.title}</h3>
                <p className={styles.cardExcerpt}>{post.excerpt}</p>
                <div className={styles.meta}>
                  <span>{post.author}</span>
                  <span className={styles.metaDot}>·</span>
                  <span>{post.date}</span>
                </div>
                <span className={styles.readMore}>Читати далі →</span>
              </div>
            </Link>
          ))}
        </div>
      </div>
    </PageLayout>
  );
}

import type { Metadata } from "next";
import { CatalogScreen } from "@/features/catalog/screens/CatalogScreen";
import type { CatalogQueryRecord } from "@/features/catalog/lib/catalog-url-params";

const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";

interface CatalogCategoryPageProps {
  params: Promise<{ slug: string }>;
  searchParams: Promise<CatalogQueryRecord>;
}

async function fetchCategoryBySlug(slug: string) {
  try {
    const res = await fetch(`${API_BASE}/catalog/categories`, {
      next: { revalidate: 3600 },
    });

    if (!res.ok) return null;

    const data = await res.json();
    const list: Array<{ name: string; slug: string; description: string | null; productCount: number }> =
      Array.isArray(data) ? data : data?.value ?? data?.items ?? data?.data ?? [];

    return list.find((c) => c.slug === slug) ?? null;
  } catch {
    return null;
  }
}

export async function generateMetadata({ params }: CatalogCategoryPageProps): Promise<Metadata> {
  const { slug } = await params;
  const category = await fetchCategoryBySlug(slug);

  if (!category) {
    return {
      title: "Каталог",
      description: "Перегляньте книги у цій категорії на Booktop.",
    };
  }

  const title = category.name;
  const description =
    category.description ||
    `${category.name} — ${category.productCount} книг(и) у категорії. Знайдіть найкращі книги на Booktop.`;

  return {
    title,
    description,
    openGraph: {
      title: `${title} | Booktop`,
      description,
      type: "website",
      url: `/catalog/${slug}`,
    },
  };
}

export default async function Page({ params, searchParams }: CatalogCategoryPageProps) {
  const { slug } = await params;
  const initialQuery = await searchParams;

  return <CatalogScreen categorySlug={slug} initialQuery={initialQuery} />;
}

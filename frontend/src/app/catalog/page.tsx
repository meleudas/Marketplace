import type { Metadata } from "next";
import { CatalogScreen } from "@/features/catalog/screens/CatalogScreen";
import type { CatalogQueryRecord } from "@/features/catalog/lib/catalog-url-params";

export const metadata: Metadata = {
  title: "Каталог книг | Book Top",
  description:
    "Широкий вибір книг в інтернет-магазині Book Top. Паперові та електронні книги. Зручний пошук, фільтри за жанром, автором, ціною. Доставка по всій Україні.",
  openGraph: {
    title: "Каталог книг | Book Top",
    description:
      "Купуйте книги в Book Top — найбільший вибір художньої, навчальної та дитячої літератури. Вигідні ціни, швидка доставка.",
    type: "website",
    url: "/catalog",
  },
};

interface CatalogPageProps {
  searchParams: Promise<CatalogQueryRecord>;
}

export default async function Page({ searchParams }: CatalogPageProps) {
  const initialQuery = await searchParams;

  return <CatalogScreen initialQuery={initialQuery} />;
}

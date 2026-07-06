import { CatalogScreen } from "@/features/catalog/screens/CatalogScreen";

interface CatalogCategoryPageProps {
  params: Promise<{ slug: string }>;
}

export default async function Page({ params }: CatalogCategoryPageProps) {
  const { slug } = await params;

  return <CatalogScreen categorySlug={slug} />;
}

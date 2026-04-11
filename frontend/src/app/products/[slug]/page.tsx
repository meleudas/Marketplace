import { ProductDetailsScreen } from "@/features/storefront/screens/ProductDetailsScreen";

interface ProductPageProps {
  params: Promise<{ slug: string }>;
}

export default async function Page({ params }: ProductPageProps) {
  const { slug } = await params;

  return <ProductDetailsScreen slug={slug} />;
}

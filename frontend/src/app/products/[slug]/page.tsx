import { ProductDetailsScreen } from "@/features/products/[slug]/screens/ProductDetailsScreen";

interface ProductPageProps {
  params: Promise<{ slug: string }>;
}

export default async function Page({ params }: ProductPageProps) {
  const { slug } = await params;

  return <ProductDetailsScreen slug={slug} />;
}

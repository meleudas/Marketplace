import type { Metadata } from "next";
import { ProductDetailsScreen } from "@/features/products/[slug]/screens/ProductDetailsScreen";

const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";

interface ProductPageProps {
  params: Promise<{ slug: string }>;
}

interface ProductListItem {
  name: string;
  description: string;
  price: number;
  imageUrls: string[];
}

async function fetchProductBySlug(slug: string) {
  try {
    const res = await fetch(`${API_BASE}/catalog/products/${slug}`, {
      next: { revalidate: 600 },
    });

    if (!res.ok) return null;

    const data = await res.json();
    const product: ProductListItem | undefined = data?.product;

    if (!product) return null;

    return {
      name: product.name,
      description: product.description,
      price: product.price,
      imageUrl: product.imageUrls?.[0] ?? null,
    };
  } catch {
    return null;
  }
}

function formatPrice(price: number): string {
  return new Intl.NumberFormat("uk-UA", { maximumFractionDigits: 0 }).format(price);
}

export async function generateMetadata({ params }: ProductPageProps): Promise<Metadata> {
  const { slug } = await params;
  const product = await fetchProductBySlug(slug);

  if (!product) {
    return {
      title: "Товар | Book Top",
      description: "Деталі товару в інтернет-магазині Book Top.",
    };
  }

  const title = product.name;
  const priceText = `${formatPrice(product.price)} грн`;
  const description = product.description
    ? `${product.description} — ${priceText}`
    : `${product.name} — ${priceText}. Купуйте в інтернет-магазині Book Top.`;

  const metadata: Metadata = {
    title,
    description,
    openGraph: {
      title: `${product.name} | Book Top`,
      description,
      type: "website",
      url: `/products/${slug}`,
      images: product.imageUrl
        ? [
            {
              url: product.imageUrl,
              alt: product.name,
            },
          ]
        : undefined,
    },
    twitter: {
      card: product.imageUrl ? "summary_large_image" : "summary",
      title: `${product.name} | Book Top`,
      description,
      images: product.imageUrl ? [product.imageUrl] : undefined,
    },
  };

  return metadata;
}

export default async function Page({ params }: ProductPageProps) {
  const { slug } = await params;

  return <ProductDetailsScreen slug={slug} />;
}

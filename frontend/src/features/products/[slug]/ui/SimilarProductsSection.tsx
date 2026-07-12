"use client";

import { useEffect, useState } from "react";
import { getSimilarProductsBySlug } from "@/features/storefront/api/catalog.api";
import { mapProductToRailCard, type ProductRailCard } from "@/features/home/lib/map-product-to-rail-card";
import { ProductRailItems } from "@/features/home/ui/ProductRailItems";
import { RecommendationsRail } from "@/features/home/ui/RecommendationsRail";

interface SimilarProductsSectionProps {
  slug: string;
}

const SIMILAR_PRODUCTS_LIMIT = 15;

export function SimilarProductsSection({ slug }: SimilarProductsSectionProps) {
  const [items, setItems] = useState<ProductRailCard[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let isCancelled = false;

    const load = async () => {
      setLoading(true);
      try {
        const products = await getSimilarProductsBySlug(slug, SIMILAR_PRODUCTS_LIMIT);
        if (!isCancelled) {
          setItems(products.map(mapProductToRailCard));
        }
      } catch {
        if (!isCancelled) {
          setItems([]);
        }
      } finally {
        if (!isCancelled) {
          setLoading(false);
        }
      }
    };

    void load();

    return () => {
      isCancelled = true;
    };
  }, [slug]);

  return (
    <RecommendationsRail title="Схожі книги" loading={loading}>
      <ProductRailItems items={items} />
    </RecommendationsRail>
  );
}

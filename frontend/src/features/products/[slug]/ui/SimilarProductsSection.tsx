"use client";

import { useEffect, useState } from "react";
import { useAddToCart } from "@/features/cart/hooks/useAddToCart";
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
  const { addToCart, addingProductId } = useAddToCart();

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
    <RecommendationsRail
      title="Схожі книги"
      variant="similar"
      loading={loading}
      itemCount={items.length}
    >
      <ProductRailItems
        items={items}
        onAddToCart={addToCart}
        addingProductId={addingProductId}
      />
    </RecommendationsRail>
  );
}

"use client";

import { ProductCard } from "@/shared/ui";
import type { ProductCardData } from "@/shared/ui";
import type { ProductRailCard } from "../lib/map-product-to-rail-card";
import styles from "./RecommendationsRail.module.css";

interface ProductRailItemsProps {
  items: ProductRailCard[];
  onAddToCart: (product: ProductCardData) => void;
  addingProductId: string | null;
}

export function ProductRailItems({
  items,
  onAddToCart,
  addingProductId,
}: ProductRailItemsProps) {
  return (
    <>
      {items.map((item) => (
        <div key={item.id} className={styles.cardLink} role="listitem">
          <ProductCard
            product={item}
            href={item.href}
            onAddToCart={onAddToCart}
            isAddingToCart={addingProductId === item.id}
          />
        </div>
      ))}
    </>
  );
}

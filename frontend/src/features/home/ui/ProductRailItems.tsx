"use client";

import { useAddToCart } from "@/features/cart/hooks/useAddToCart";
import { AddToCartDialog } from "@/features/cart/ui/AddToCartDialog";
import { ProductCard } from "@/shared/ui";
import type { ProductRailCard } from "../lib/map-product-to-rail-card";
import styles from "./RecommendationsRail.module.css";

interface ProductRailItemsProps {
  items: ProductRailCard[];
}

export function ProductRailItems({ items }: ProductRailItemsProps) {
  const { addToCart, addingProductId, addedProduct, dismissAddedDialog } = useAddToCart();

  return (
    <>
      {items.map((item) => (
        <div key={item.id} className={styles.cardLink} role="listitem">
          <ProductCard
            product={item}
            href={item.href}
            onAddToCart={addToCart}
            isAddingToCart={addingProductId === item.id}
          />
        </div>
      ))}

      <AddToCartDialog
        open={addedProduct !== null}
        product={addedProduct}
        onClose={dismissAddedDialog}
      />
    </>
  );
}

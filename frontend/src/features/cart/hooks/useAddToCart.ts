"use client";

import { AxiosError } from "axios";
import { useCallback, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { addCartItem } from "@/features/cart/api/cart.api";
import type { AddedToCartProduct } from "@/features/cart/model/added-to-cart.types";
import { useCartStore } from "@/features/cart/model/cart.store";

export type AddToCartInput = Pick<AddedToCartProduct, "id" | "title" | "imageUrl" | "price">;

export function useAddToCart() {
  const router = useRouter();
  const pathname = usePathname();
  const loadCart = useCartStore((state) => state.loadCart);
  const setTotalItems = useCartStore((state) => state.setTotalItems);
  const [addingProductId, setAddingProductId] = useState<string | null>(null);
  const [addedProduct, setAddedProduct] = useState<AddedToCartProduct | null>(null);

  const dismissAddedDialog = useCallback(() => {
    setAddedProduct(null);
  }, []);

  const addToCart = useCallback(
    async (product: AddToCartInput) => {
      const numericProductId = Number(product.id);
      if (!Number.isFinite(numericProductId)) {
        return;
      }

      setAddingProductId(product.id);

      try {
        const cart = await addCartItem({ productId: numericProductId, quantity: 1 });
        setTotalItems(cart.totalItems);
        await loadCart();
        setAddedProduct({
          id: product.id,
          title: product.title,
          imageUrl: product.imageUrl,
          price: product.price,
        });
      } catch (error) {
        const status = error instanceof AxiosError ? error.response?.status : undefined;

        if (status === 401) {
          const redirect = encodeURIComponent(pathname || "/");
          router.push(`/auth/login?redirect=${redirect}`);
          return;
        }

        console.error(error);
      } finally {
        setAddingProductId(null);
      }
    },
    [loadCart, pathname, router, setTotalItems],
  );

  return { addToCart, addingProductId, addedProduct, dismissAddedDialog };
}

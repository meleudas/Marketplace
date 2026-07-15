"use client";

import { useSyncExternalStore } from "react";
import { getGuestCartTotalItems, subscribeToGuestCart } from "@/features/cart/lib/guest-cart.storage";

const getServerSnapshot = () => 0;

export function useGuestCartCount(): number {
  return useSyncExternalStore(subscribeToGuestCart, getGuestCartTotalItems, getServerSnapshot);
}

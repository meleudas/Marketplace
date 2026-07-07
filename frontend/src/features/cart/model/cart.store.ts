"use client";

import { create } from "zustand";
import { apiClient } from "@/shared/api/http.client";

interface CartState {
  totalItems: number;
  initialized: boolean;
  loadCart: () => Promise<void>;
  setTotalItems: (count: number) => void;
  clear: () => void;
}

interface CartResponse {
  totalItems: number;
}

export const useCartStore = create<CartState>((set, get) => ({
  totalItems: 0,
  initialized: false,

  loadCart: async () => {
    try {
      const response = await apiClient.get<CartResponse>("/me/cart");
      set({ totalItems: response.data.totalItems, initialized: true });
    } catch {
      set({ totalItems: 0, initialized: true });
    }
  },

  setTotalItems: (count: number) => {
    set({ totalItems: count });
  },

  clear: () => {
    set({ totalItems: 0, initialized: false });
  },
}));

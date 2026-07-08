import { apiClient } from "@/shared/api/http.client";
import type { CartDto } from "@/features/checkout/api/checkout.api";

export interface AddCartItemPayload {
  productId: number;
  quantity?: number;
}

export const addCartItem = async ({
  productId,
  quantity = 1,
}: AddCartItemPayload): Promise<CartDto> => {
  const response = await apiClient.post<CartDto>("/me/cart/items", {
    productId,
    quantity,
  });
  return response.data;
};

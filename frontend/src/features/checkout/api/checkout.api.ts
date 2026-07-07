import { apiClient } from "@/shared/api/http.client";

export interface CartItemDto {
  id: number;
  productId: number;
  quantity: number;
  priceAtMoment: number;
  discount: number;
  lineTotal: number;
}

export interface CartDto {
  id: number;
  userId: string;
  lastActivityAt: string;
  items: CartItemDto[];
  totalItems: number;
  totalAmount: number;
}

export interface ShippingMethodDto {
  id: number;
  name: string;
  carrierCode: string;
  price: number;
  freeShippingThreshold: number | null;
  estimatedDaysMin: number;
  estimatedDaysMax: number;
}

export interface CheckoutAddressRequest {
  firstName: string;
  lastName: string;
  phone: string;
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
}

export interface CheckoutCartRequest {
  paymentMethod: string; // "Card", "PayPal", "BankTransfer", "Cash"
  shippingMethodId: number;
  address: CheckoutAddressRequest;
  notes?: string;
}

export interface PaymentInitDto {
  provider: string;
  status: string;
  transactionId: string | null;
  data: string;
  signature: string;
  checkoutUrl: string | null;
}

export interface CreatedOrderDto {
  orderId: number;
  orderNumber: string;
  companyId: string;
  status: number;
  itemCount: number;
  totalPrice: number;
  payment: PaymentInitDto | null;
}

export interface CheckoutResultDto {
  createdOrders: CreatedOrderDto[];
}

export const fetchMyCart = async (): Promise<CartDto> => {
  const response = await apiClient.get<CartDto>("/me/cart");
  return response.data;
};

export const fetchShippingMethods = async (): Promise<ShippingMethodDto[]> => {
  const response = await apiClient.get<ShippingMethodDto[]>("/shipping/methods");
  return response.data;
};

export const submitCheckout = async (
  payload: CheckoutCartRequest,
  idempotencyKey: string,
): Promise<CheckoutResultDto> => {
  const response = await apiClient.post<CheckoutResultDto>("/me/cart/checkout", payload, {
    headers: {
      "Idempotency-Key": idempotencyKey,
    },
  });
  return response.data;
};

export interface NovaPoshtaCity {
  Description: string;
  Ref: string;
}

export interface NovaPoshtaWarehouse {
  Description: string;
  Ref: string;
  Number: string;
}

export const fetchNovaPoshtaCities = async (query: string): Promise<NovaPoshtaCity[]> => {
  const response = await fetch("https://api.novaposhta.ua/v2.0/json/", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      apiKey: "",
      modelName: "Address",
      calledMethod: "getCities",
      methodProperties: {
        FindByString: query,
        Limit: 20,
      },
    }),
  });
  const result = await response.json();
  if (result.success) {
    return result.data;
  }
  return [];
};

export const fetchNovaPoshtaWarehouses = async (cityRef: string): Promise<NovaPoshtaWarehouse[]> => {
  const response = await fetch("https://api.novaposhta.ua/v2.0/json/", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      apiKey: "",
      modelName: "Address",
      calledMethod: "getWarehouses",
      methodProperties: {
        CityRef: cityRef,
      },
    }),
  });
  const result = await response.json();
  if (result.success) {
    return result.data;
  }
  return [];
};

export const updateCartItemQuantity = async (itemId: number, quantity: number): Promise<CartDto> => {
  const response = await apiClient.patch<CartDto>(`/me/cart/items/${itemId}`, { quantity });
  return response.data;
};

export const removeCartItem = async (itemId: number): Promise<CartDto> => {
  const response = await apiClient.delete<CartDto>(`/me/cart/items/${itemId}`);
  return response.data;
};

import { apiClient } from "@/shared/api/http.client";

export interface UserAddress {
  id: number;
  type: string;
  isDefault: boolean;
  firstName: string;
  lastName: string;
  phone: string;
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
}

export interface UpsertAddressPayload {
  type: string;
  isDefault: boolean;
  firstName: string;
  lastName: string;
  phone: string;
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
}

export interface OrderItemDto {
  orderItemId: number;
  productId: number;
  productName: string;
  productImage: string | null;
  quantity: number;
  priceAtMoment: number;
  discount: number;
  totalPrice: number;
}

export interface OrderAddressDto {
  kind: string;
  firstName: string;
  lastName: string;
  phone: string;
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
}

export interface OrderListItem {
  orderId: number;
  orderNumber: string;
  customerId: string;
  companyId: string;
  status: string;
  totalPrice: number;
  paymentMethod: string;
  createdAt: string;
  updatedAt: string;
}

export interface PagedOrdersResponse {
  items: OrderListItem[];
  total: number;
  page: number;
  pageSize: number;
}

export interface PaymentSnapshotDto {
  paymentId: number;
  method: string;
  amount: number;
  currency: string;
  transactionId: string | null;
  status: string;
  processedAt: string | null;
}

export interface RefundSnapshotDto {
  refundId: number;
  amount: number;
  reason: string;
  status: string;
  processedByUserId: string | null;
  processedAt: string | null;
  createdAt: string;
}

export interface ReturnLineItemDto {
  returnLineItemId: number;
  orderItemId: number;
  quantity: number;
  reasonCode: string;
  status: string;
}

export interface ReturnSnapshotDto {
  returnId: number;
  status: string;
  reasonCode: string;
  createdAt: string;
  receivedAtUtc: string | null;
  refundId: number | null;
  lines: ReturnLineItemDto[];
}

export interface OrderStatusHistoryDto {
  oldStatus: string;
  newStatus: string;
  changedByUserId: string;
  actorRole: string | null;
  source: string;
  comment: string | null;
  correlationId: string | null;
  changedAt: string;
}

export interface OrderDetails {
  orderId: number;
  orderNumber: string;
  customerId: string;
  companyId: string;
  status: string;
  totalPrice: number;
  subtotal: number;
  shippingCost: number;
  discountAmount: number;
  taxAmount: number;
  paymentMethod: string;
  notes: string | null;
  trackingNumber: string | null;
  shippedAt: string | null;
  deliveredAt: string | null;
  cancelledAt: string | null;
  refundedAt: string | null;
  createdAt: string;
  updatedAt: string;
  items: OrderItemDto[];
  addresses: OrderAddressDto[];
  payment: PaymentSnapshotDto | null;
  refunds: RefundSnapshotDto[];
  returns: ReturnSnapshotDto[];
  statusHistory: OrderStatusHistoryDto[];
}

export const fetchAddresses = async (): Promise<UserAddress[]> => {
  const response = await apiClient.get<UserAddress[]>("/me/addresses");
  return response.data;
};

export const createAddress = async (payload: UpsertAddressPayload): Promise<UserAddress> => {
  const response = await apiClient.post<UserAddress>("/me/addresses", payload);
  return response.data;
};

export const deleteAddress = async (addressId: number): Promise<void> => {
  await apiClient.delete(`/me/addresses/${addressId}`);
};

export const fetchOrders = async (): Promise<PagedOrdersResponse> => {
  const response = await apiClient.get<PagedOrdersResponse>("/me/orders");
  return response.data;
};

export const fetchOrderDetails = async (orderId: number): Promise<OrderDetails> => {
  const response = await apiClient.get<OrderDetails>(`/me/orders/${orderId}`);
  return response.data;
};

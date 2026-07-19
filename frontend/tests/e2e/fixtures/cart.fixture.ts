import type { APIRequestContext } from "@playwright/test";
import { getApiBaseUrl } from "./backend.helper";

export interface CartProductFixture {
  id: number;
  name: string;
  slug: string;
  price: number;
}

interface CatalogSearchResponse {
  items?: Array<{
    id: number;
    name: string;
    slug: string;
    price: number;
    availabilityStatus?: string;
    availableQty?: number;
  }>;
}

interface CartApiResponse {
  items?: Array<{ id: number; productId: number; quantity: number }>;
}

async function getJson(request: APIRequestContext, path: string): Promise<unknown> {
  const response = await request.get(`${getApiBaseUrl()}${path}`);
  if (!response.ok()) {
    throw new Error(`Cart fixture request failed (${response.status()}): ${path}`);
  }

  return response.json();
}

export async function getInStockCatalogProduct(
  request: APIRequestContext,
): Promise<CartProductFixture> {
  const payload = (await getJson(
    request,
    "/catalog/products/search?page=1&pageSize=40",
  )) as CatalogSearchResponse;

  const product = (payload.items ?? []).find((item) => {
    const status = String(item.availabilityStatus ?? "").toLowerCase();
    const availableQty = item.availableQty ?? 1;
    return status !== "out_of_stock" && availableQty > 0;
  });

  if (!product) {
    throw new Error("Cart fixture requires at least one in-stock catalog product.");
  }

  return {
    id: product.id,
    name: product.name,
    slug: product.slug,
    price: product.price,
  };
}

export async function clearAuthenticatedCart(
  request: APIRequestContext,
  accessToken: string,
): Promise<void> {
  const apiBase = getApiBaseUrl();
  const cartResponse = await request.get(`${apiBase}/me/cart`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });

  if (!cartResponse.ok()) {
    return;
  }

  const cart = (await cartResponse.json()) as CartApiResponse;
  for (const item of cart.items ?? []) {
    await request.delete(`${apiBase}/me/cart/items/${item.id}`, {
      headers: { Authorization: `Bearer ${accessToken}` },
    });
  }
}

export function formatExpectedCartPrice(unitPrice: number, quantity: number): string {
  const lineTotal = unitPrice * quantity;
  const factor = 100;
  const rounded = Math.ceil(Number(lineTotal.toFixed(10)) * factor) / factor;
  return `${rounded.toFixed(2)} грн.`;
}

import { createHash } from "node:crypto";
import type { APIRequestContext, Page } from "@playwright/test";
import { getApiBaseUrl } from "./backend.helper";
import {
  clearAuthenticatedCart,
  getInStockCatalogProduct,
  type CartProductFixture,
} from "./cart.fixture";

export interface CheckoutAddressFixture {
  firstName: string;
  lastName: string;
  phone: string;
  city: string;
  postalCode: string;
  branch: string;
}

export interface CreatedCheckoutOrder {
  orderId: number;
  orderNumber: string;
  totalPrice: number;
  payment: {
    status: string;
    transactionId: string | null;
    checkoutUrl: string | null;
  } | null;
}

export interface CheckoutResultDto {
  createdOrders: CreatedCheckoutOrder[];
}

export const checkoutTestAddress: CheckoutAddressFixture = {
  firstName: "Тест",
  lastName: "Покупець",
  phone: "+380501112233",
  city: "Київ",
  postalCode: "01001",
  branch: "Відділення №1",
};

export function getLiqPaySandboxPrivateKey(): string | null {
  return (
    process.env.LIQPAY_SANDBOX_PRIVATE ??
    process.env.LIQPAY__PRIVATEKEY ??
    process.env.LiqPay__PrivateKey ??
    null
  );
}

export function skipIfLiqPaySandboxUnavailable(): boolean {
  return !getLiqPaySandboxPrivateKey();
}

export function buildLiqPayWebhookPayload(
  orderId: string,
  status: "success" | "failure" | "error",
  privateKey: string,
): { data: string; signature: string } {
  const payloadJson = JSON.stringify({ order_id: orderId, status });
  const data = Buffer.from(payloadJson, "utf8").toString("base64");
  const signature = createHash("sha1")
    .update(`${privateKey}${data}${privateKey}`, "utf8")
    .digest("base64");
  return { data, signature };
}

export async function postLiqPayWebhook(
  request: APIRequestContext,
  orderId: string,
  status: "success" | "failure" | "error",
): Promise<void> {
  const privateKey = getLiqPaySandboxPrivateKey();
  if (!privateKey) {
    throw new Error("LiqPay sandbox private key is not configured for E2E.");
  }

  const body = buildLiqPayWebhookPayload(orderId, status, privateKey);
  const response = await request.post(`${getApiBaseUrl()}/integrations/liqpay/webhook`, {
    data: body,
  });

  if (!response.ok()) {
    throw new Error(
      `LiqPay webhook failed (${response.status()}): ${await response.text()}`,
    );
  }
}

export async function seedAuthenticatedCart(
  request: APIRequestContext,
  accessToken: string,
  product?: CartProductFixture,
): Promise<CartProductFixture> {
  await clearAuthenticatedCart(request, accessToken);
  const item = product ?? (await getInStockCatalogProduct(request));

  const response = await request.post(`${getApiBaseUrl()}/me/cart/items`, {
    headers: { Authorization: `Bearer ${accessToken}` },
    data: { productId: item.id, quantity: 1 },
  });

  if (!response.ok()) {
    throw new Error(`Failed to seed cart (${response.status()}): ${await response.text()}`);
  }

  return item;
}

export async function getAuthenticatedCartItemCount(
  request: APIRequestContext,
  accessToken: string,
): Promise<number> {
  const response = await request.get(`${getApiBaseUrl()}/me/cart`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });

  if (!response.ok()) {
    return 0;
  }

  const cart = (await response.json()) as { totalItems?: number; items?: unknown[] };
  if (typeof cart.totalItems === "number") {
    return cart.totalItems;
  }

  return Array.isArray(cart.items) ? cart.items.length : 0;
}

export async function getOrderPaymentStatus(
  request: APIRequestContext,
  accessToken: string,
  orderNumber: string,
): Promise<string | null> {
  const listResponse = await request.get(`${getApiBaseUrl()}/me/orders`, {
    headers: { Authorization: `Bearer ${accessToken}` },
    params: { search: orderNumber },
  });

  if (!listResponse.ok()) {
    throw new Error(`Failed to list orders (${listResponse.status()})`);
  }

  const list = (await listResponse.json()) as {
    items?: Array<{ orderId: number; orderNumber: string }>;
  };
  const matched = (list.items ?? []).find(
    (item) => item.orderNumber.toLowerCase() === orderNumber.toLowerCase(),
  );

  if (!matched) {
    return null;
  }

  const detailsResponse = await request.get(`${getApiBaseUrl()}/me/orders/${matched.orderId}`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });

  if (!detailsResponse.ok()) {
    throw new Error(`Failed to load order details (${detailsResponse.status()})`);
  }

  const details = (await detailsResponse.json()) as {
    payment?: { status?: string } | null;
  };

  return details.payment?.status ?? null;
}

/**
 * Rewrites LiqPay checkoutUrl to the local result page so E2E never opens the real payment UI.
 * Returns a getter for the last successful checkout payload.
 */
export async function installCheckoutPaymentIntercept(page: Page): Promise<{
  getLastCheckout: () => CheckoutResultDto | null;
}> {
  let lastCheckout: CheckoutResultDto | null = null;
  const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:3000";

  await page.route("**/me/cart/checkout", async (route) => {
    if (route.request().method() !== "POST") {
      await route.continue();
      return;
    }

    const response = await route.fetch();
    const raw = await response.text();
    let json: CheckoutResultDto;

    try {
      json = JSON.parse(raw) as CheckoutResultDto;
    } catch {
      await route.fulfill({
        status: response.status(),
        headers: response.headers(),
        body: raw,
      });
      return;
    }

    lastCheckout = json;

    for (const order of json.createdOrders ?? []) {
      if (order.payment) {
        order.payment.checkoutUrl = `${baseURL}/checkout/result?order_id=${encodeURIComponent(order.orderNumber)}`;
      }
    }

    await route.fulfill({
      status: response.status(),
      headers: {
        ...response.headers(),
        "content-type": "application/json",
      },
      body: JSON.stringify(json),
    });
  });

  return {
    getLastCheckout: () => lastCheckout,
  };
}

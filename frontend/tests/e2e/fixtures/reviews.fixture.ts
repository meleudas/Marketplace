import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";
import path from "node:path";
import type { APIRequestContext } from "@playwright/test";
import { getApiBaseUrl } from "./backend.helper";
import {
  clearAuthenticatedCart,
  getInStockCatalogProduct,
  type CartProductFixture,
} from "./cart.fixture";
import {
  checkoutTestAddress,
  getOrderPaymentStatus,
  postLiqPayWebhook,
  type CheckoutResultDto,
} from "./checkout.fixture";
import { loginUserViaApi } from "./api.helper";
import { testUsers } from "./users.fixture";

export interface ProductReviewDto {
  id: number;
  userId: string;
  userName: string;
  comment: string;
  createdAt: string;
  rating: number | null;
}

interface ProductReviewListDto {
  items?: ProductReviewDto[];
}

interface ShippingMethodDto {
  id: number;
}

const UUID_RE =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function authHeaders(accessToken: string): Record<string, string> {
  return { Authorization: `Bearer ${accessToken}` };
}

function runPostgresSql(sql: string): void {
  const repoRoot = path.resolve(process.cwd(), "..");
  const composeFile = path.join(repoRoot, "docker-compose.dev.yml");

  execFileSync(
    "docker",
    [
      "compose",
      "-f",
      composeFile,
      "exec",
      "-T",
      "postgres",
      "psql",
      "-U",
      "postgres",
      "-d",
      "marketplace",
      "-v",
      "ON_ERROR_STOP=1",
      "-c",
      sql,
    ],
    { cwd: repoRoot, stdio: ["ignore", "pipe", "pipe"] },
  );
}

function clearProductReviewCache(productId: number): void {
  try {
    execFileSync(
      "docker",
      [
        "exec",
        "marketplace-redis-1",
        "redis-cli",
        "DEL",
        `catalog:products:reviews:${productId}:1:20`,
        `catalog:products:reviews:${productId}:1:100`,
      ],
      { stdio: ["ignore", "pipe", "pipe"] },
    );
  } catch {
    // Redis may be unavailable in some environments; list endpoints still work uncached after TTL.
  }
}

/**
 * Hard-deletes E2E review rows so the same seed buyer can create again.
 * Soft-delete via API blocks recreate without backend changes.
 */
export function purgeProductReviewsForUser(productId: number, userId: string): void {
  if (!Number.isFinite(productId) || !UUID_RE.test(userId)) {
    throw new Error("purgeProductReviewsForUser requires a valid productId and user UUID.");
  }

  runPostgresSql(
    `DELETE FROM product_reviews WHERE "ProductId" = ${productId} AND "UserId" = '${userId}';`,
  );
  clearProductReviewCache(productId);
  clearReviewCreateRateLimit(userId, productId);
}

/** Clears Redis anti-abuse counter so presentation/E2E can retry create on the same product. */
export function clearReviewCreateRateLimit(userId: string, productId: number): void {
  if (!Number.isFinite(productId) || !UUID_RE.test(userId)) {
    return;
  }

  const compactUserId = userId.replaceAll("-", "").toLowerCase();
  try {
    execFileSync(
      "docker",
      [
        "exec",
        "marketplace-redis-1",
        "redis-cli",
        "DEL",
        `ratelimit:review-create:${compactUserId}:${productId}`,
      ],
      { stdio: ["ignore", "pipe", "pipe"] },
    );
  } catch {
    // Redis may be unavailable; create may still succeed within the daily budget.
  }
}

export async function loginReviewBuyer(): Promise<{
  email: string;
  password: string;
  accessToken: string;
}> {
  const credentials = testUsers.reviewBuyer;
  const tokens = await loginUserViaApi(credentials.email, credentials.password);
  return { ...credentials, accessToken: tokens.accessToken };
}

export async function loginReviewNonBuyer(): Promise<{
  email: string;
  password: string;
  accessToken: string;
}> {
  const credentials = testUsers.reviewNonBuyer;
  const tokens = await loginUserViaApi(credentials.email, credentials.password);
  return { ...credentials, accessToken: tokens.accessToken };
}

export async function listProductReviews(
  request: APIRequestContext,
  productId: number,
): Promise<ProductReviewDto[]> {
  const response = await request.get(
    `${getApiBaseUrl()}/products/${productId}/reviews?page=1&size=20`,
  );
  if (!response.ok()) {
    throw new Error(`Failed to list reviews (${response.status()})`);
  }

  const payload = (await response.json()) as ProductReviewListDto;
  return payload.items ?? [];
}

export async function getCurrentUserProfile(
  request: APIRequestContext,
  accessToken: string,
): Promise<{ id: string; email: string; firstName: string }> {
  const response = await request.get(`${getApiBaseUrl()}/users/me`, {
    headers: authHeaders(accessToken),
  });
  if (!response.ok()) {
    throw new Error(`Failed to load current user (${response.status()})`);
  }

  const user = (await response.json()) as { id: string; email: string; firstName: string };
  return user;
}

export async function ensurePaidPurchaseForProduct(
  request: APIRequestContext,
  accessToken: string,
  product: CartProductFixture,
): Promise<void> {
  await clearAuthenticatedCart(request, accessToken);

  const addResponse = await request.post(`${getApiBaseUrl()}/me/cart/items`, {
    headers: authHeaders(accessToken),
    data: { productId: product.id, quantity: 1 },
  });
  if (!addResponse.ok()) {
    throw new Error(`Failed to add cart item (${addResponse.status()}): ${await addResponse.text()}`);
  }

  const shippingResponse = await request.get(`${getApiBaseUrl()}/shipping/methods`);
  if (!shippingResponse.ok()) {
    throw new Error(`Failed to load shipping methods (${shippingResponse.status()})`);
  }
  const shippingMethods = (await shippingResponse.json()) as ShippingMethodDto[];
  const shippingMethodId = shippingMethods[0]?.id;
  if (!shippingMethodId) {
    throw new Error("No shipping methods available for review purchase setup.");
  }

  const checkoutResponse = await request.post(`${getApiBaseUrl()}/me/cart/checkout`, {
    headers: {
      ...authHeaders(accessToken),
      "Idempotency-Key": randomUUID(),
      "Content-Type": "application/json",
    },
    data: {
      paymentMethod: "Card",
      shippingMethodId,
      address: {
        firstName: checkoutTestAddress.firstName,
        lastName: checkoutTestAddress.lastName,
        phone: checkoutTestAddress.phone,
        street: checkoutTestAddress.branch,
        city: checkoutTestAddress.city,
        state: "UA-UP",
        postalCode: checkoutTestAddress.postalCode,
        country: "Україна",
      },
    },
  });

  if (!checkoutResponse.ok()) {
    throw new Error(
      `Checkout for review purchase failed (${checkoutResponse.status()}): ${await checkoutResponse.text()}`,
    );
  }

  const checkout = (await checkoutResponse.json()) as CheckoutResultDto;
  const order = checkout.createdOrders?.[0];
  if (!order?.orderNumber) {
    throw new Error("Checkout did not return a created order.");
  }

  const paymentRef = order.payment?.transactionId || order.orderNumber;
  await postLiqPayWebhook(request, paymentRef, "success");

  const deadline = Date.now() + 20_000;
  while (Date.now() < deadline) {
    const status = await getOrderPaymentStatus(request, accessToken, order.orderNumber);
    if (status && /completed|paid|success|captured/i.test(status)) {
      return;
    }
    await new Promise((resolve) => setTimeout(resolve, 500));
  }

  throw new Error(`Paid purchase was not confirmed for order ${order.orderNumber}.`);
}

export async function prepareReviewProduct(
  request: APIRequestContext,
): Promise<CartProductFixture> {
  return getInStockCatalogProduct(request);
}

/** Product ids from the buyer's recent orders (for excluding verified purchases). */
export async function listPurchasedProductIds(
  request: APIRequestContext,
  accessToken: string,
  orderPages = 3,
): Promise<Set<number>> {
  const ids = new Set<number>();
  const headers = authHeaders(accessToken);

  for (let page = 1; page <= orderPages; page += 1) {
    const ordersResponse = await request.get(`${getApiBaseUrl()}/me/orders`, {
      headers,
      params: { page, pageSize: 10 },
    });
    if (!ordersResponse.ok()) {
      break;
    }

    const ordersPayload = (await ordersResponse.json()) as {
      items?: Array<{ orderId: number }>;
    };
    const orders = ordersPayload.items ?? [];
    if (orders.length === 0) {
      break;
    }

    for (const order of orders) {
      const detailsResponse = await request.get(
        `${getApiBaseUrl()}/me/orders/${order.orderId}`,
        { headers },
      );
      if (!detailsResponse.ok()) {
        continue;
      }
      const details = (await detailsResponse.json()) as {
        items?: Array<{ productId: number }>;
      };
      for (const line of details.items ?? []) {
        if (line.productId) {
          ids.add(line.productId);
        }
      }
    }
  }

  return ids;
}

/**
 * In-stock catalog product the buyer has not purchased (for “verified purchase required”).
 */
export async function getUnpurchasedCatalogProduct(
  request: APIRequestContext,
  accessToken: string,
  excludeIds: Iterable<number> = [],
): Promise<CartProductFixture> {
  const purchasedIds = await listPurchasedProductIds(request, accessToken);
  for (const id of excludeIds) {
    purchasedIds.add(id);
  }

  const searchResponse = await request.get(`${getApiBaseUrl()}/catalog/products/search`, {
    params: { page: 1, pageSize: 80 },
  });
  if (!searchResponse.ok()) {
    throw new Error(`Catalog search failed (${searchResponse.status()})`);
  }

  const search = (await searchResponse.json()) as {
    items?: Array<{
      id: number;
      name: string;
      slug: string;
      price: number;
      availabilityStatus?: string;
      availableQty?: number;
    }>;
  };

  const match = (search.items ?? []).find((item) => {
    if (purchasedIds.has(item.id)) {
      return false;
    }
    const status = String(item.availabilityStatus ?? "").toLowerCase();
    const availableQty = item.availableQty ?? 1;
    return status !== "out_of_stock" && availableQty > 0;
  });

  if (!match) {
    throw new Error("No in-stock catalog product left that the buyer has not purchased.");
  }

  return {
    id: match.id,
    name: match.name,
    slug: match.slug,
    price: match.price,
  };
}

/**
 * Resolves a purchased catalog product from the buyer's latest order
 * (used so presentation scenario 04 continues from scenario 03).
 */
export async function getLatestPurchasedProduct(
  request: APIRequestContext,
  accessToken: string,
): Promise<CartProductFixture | null> {
  const ordersResponse = await request.get(`${getApiBaseUrl()}/me/orders`, {
    headers: authHeaders(accessToken),
    params: { page: 1, pageSize: 5 },
  });
  if (!ordersResponse.ok()) {
    return null;
  }

  const ordersPayload = (await ordersResponse.json()) as {
    items?: Array<{ orderId: number }>;
  };
  const latest = ordersPayload.items?.[0];
  if (!latest?.orderId) {
    return null;
  }

  const detailsResponse = await request.get(
    `${getApiBaseUrl()}/me/orders/${latest.orderId}`,
    { headers: authHeaders(accessToken) },
  );
  if (!detailsResponse.ok()) {
    return null;
  }

  const details = (await detailsResponse.json()) as {
    items?: Array<{ productId: number; productName: string; priceAtMoment?: number }>;
  };
  const line = details.items?.[0];
  if (!line?.productId) {
    return null;
  }

  const searchResponse = await request.get(`${getApiBaseUrl()}/catalog/products/search`, {
    params: { page: 1, pageSize: 100 },
  });
  if (!searchResponse.ok()) {
    return null;
  }

  const search = (await searchResponse.json()) as {
    items?: Array<{ id: number; name: string; slug: string; price: number }>;
  };
  const match = (search.items ?? []).find((item) => item.id === line.productId);
  if (!match) {
    return null;
  }

  return {
    id: match.id,
    name: match.name || line.productName,
    slug: match.slug,
    price: match.price || line.priceAtMoment || 0,
  };
}

export function formatExpectedReviewDate(isoDate: string): string {
  return new Date(isoDate).toLocaleDateString("uk-UA", {
    day: "numeric",
    month: "long",
    year: "numeric",
  });
}

export function createUniqueReviewComment(prefix = "E2E review"): string {
  return `${prefix} ${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
}

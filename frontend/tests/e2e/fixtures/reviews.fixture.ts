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

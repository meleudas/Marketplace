/**
 * Buyer-flow presentation recordings against the real local Docker backend.
 *
 * Rules:
 * - No page.route() business API mocks
 * - Browser → Next.js → backend API → database
 * - Credentials from `.env.e2e` / env vars only
 */
import fs from "node:fs";
import path from "node:path";
import { ASSETS, expect, hold, test } from "../helpers/record-clip";
import {
  createApiProofCollector,
  writeProofReport,
  type ProofRow,
} from "../helpers/backend-proof";
import { clearAuthState, loginViaUi } from "../../e2e/fixtures/auth.fixture";
import {
  getVerifiedTestCredentials,
  loginUserViaApi,
} from "../../e2e/fixtures/api.helper";
import { getApiBaseUrl } from "../../e2e/fixtures/backend.helper";
import { getCatalogTestData } from "../../e2e/fixtures/catalog.fixture";
import { clearAuthenticatedCart } from "../../e2e/fixtures/cart.fixture";
import {
  getAuthenticatedCartItemCount,
  getOrderPaymentStatus,
  postLiqPayWebhook,
  seedAuthenticatedCart,
  skipIfLiqPaySandboxUnavailable,
} from "../../e2e/fixtures/checkout.fixture";
import {
  createUniqueReviewComment,
  ensurePaidPurchaseForProduct,
  getCurrentUserProfile,
  listProductReviews,
  loginReviewBuyer,
  prepareReviewProduct,
  purgeProductReviewsForUser,
} from "../../e2e/fixtures/reviews.fixture";
import { testUsers } from "../../e2e/fixtures/users.fixture";
import { CatalogPage } from "../../e2e/pages/catalog.page";
import { CartPage, CatalogCartActions } from "../../e2e/pages/cart.page";
import { CheckoutPage, CheckoutResultPage } from "../../e2e/pages/checkout.page";
import { ProductReviewsPage } from "../../e2e/pages/product-reviews.page";

const REPO_ROOT = path.resolve(__dirname, "../../../../");
const PROOF_PATH = path.join(REPO_ROOT, "presentation-assets", "flows", "backend-proof.md");
const MANIFEST_META_PATH = path.join(
  REPO_ROOT,
  "presentation-assets",
  "flows",
  ".record-meta.json",
);

type FlowMeta = {
  id: string;
  title: string;
  path: string;
  durationSec: number;
  userEmail: string;
  endpoints: string[];
  persistenceConfirmed: boolean;
  status: "real backend" | "not recorded";
  shown: string;
  note?: string;
};

const proofRows: ProofRow[] = [];
const flowMetas: FlowMeta[] = [];

async function assertBackendReady(): Promise<void> {
  const health = await fetch(`${getApiBaseUrl()}/health`, {
    signal: AbortSignal.timeout(5_000),
  });
  if (!health.ok) {
    throw new Error(`Backend health failed: ${health.status} at ${getApiBaseUrl()}/health`);
  }
}

test.describe.configure({ mode: "serial" });

test.describe("Buyer flows — real backend", () => {
  test.beforeAll(async () => {
    await assertBackendReady();
    fs.mkdirSync(ASSETS.flows, { recursive: true });
  });

  test.afterAll(() => {
    writeProofReport(PROOF_PATH, proofRows, {
      "API base": getApiBaseUrl(),
      Frontend: process.env.PLAYWRIGHT_BASE_URL ?? "http://127.0.0.1:3000",
      "Mock routes": "none (page.route not used)",
    });
    fs.writeFileSync(MANIFEST_META_PATH, JSON.stringify(flowMetas, null, 2), "utf8");
  });

  test("01-catalog", async ({ recordClip, request }) => {
    const catalogData = await getCatalogTestData(request);
    let persistence = false;
    let endpoints: string[] = [];

    const { durationSec } = await recordClip(
      path.join(ASSETS.flows, "01-catalog.mp4"),
      async (page) => {
        await clearAuthState(page);
        const proof = createApiProofCollector(page);
        const catalog = new CatalogPage(page);

        await catalog.goto();
        await hold(page, 2500);

        await catalog.applyFormat("Паперова", "паперовий");
        await hold(page, 2500);
        await catalog.applyAuthor(catalogData.author.label, catalogData.author.value);
        await hold(page, 2500);

        await expect(page).toHaveURL(/format=|authors=/);
        await catalog.expectSelectedFilter("Паперова");
        await catalog.expectSelectedFilter(catalogData.author.label);

        persistence = proof.rows.some(
          (row) =>
            row.url.includes("/catalog/products/search") &&
            row.status >= 200 &&
            row.status < 300,
        );
        const summary = proof.summarize("01-catalog", persistence);
        endpoints = summary.endpoints;
        proofRows.push(summary);
        await hold(page, 3000);
      },
    );

    flowMetas.push({
      id: "01-catalog",
      title: "Каталог і фільтри",
      path: "presentation-assets/flows/01-catalog.mp4",
      durationSec,
      userEmail: "(guest)",
      endpoints,
      persistenceConfirmed: persistence,
      status: persistence ? "real backend" : "not recorded",
      shown: "Каталог → фільтр формату «Паперова» → автор → обрані фільтри / URL",
    });
  });

  test("02-guest-cart", async ({ recordClip }) => {
    let persistence = false;
    let endpoints: string[] = [];
    let productSlug = "";

    const { durationSec } = await recordClip(
      path.join(ASSETS.flows, "02-guest-cart.mp4"),
      async (page) => {
        await clearAuthState(page);
        const proof = createApiProofCollector(page);
        const catalogCart = new CatalogCartActions(page);
        const cart = new CartPage(page);

        const product = await catalogCart.addFirstVisibleCatalogProduct();
        productSlug = product.slug;
        await hold(page, 2500);
        await expect(page.getByTestId("header-cart-badge")).toHaveText("1");
        await catalogCart.goToCartFromDialog();
        await hold(page, 2500);

        await page.getByRole("button", { name: "Збільшити кількість" }).click();
        await expect(page.getByTestId("cart-item-quantity").first()).toHaveText("2");
        await cart.expectHeaderCount(2);
        await hold(page, 3000);

        const guestCart = await page.evaluate(() => window.localStorage.getItem("guestCart"));
        persistence = Boolean(guestCart) && proof.rows.some((r) => r.url.includes("/catalog/"));
        const summary = proof.summarize("02-guest-cart", persistence);
        endpoints = summary.endpoints;
        proofRows.push(summary);
      },
    );

    flowMetas.push({
      id: "02-guest-cart",
      title: "Гостьовий кошик",
      path: "presentation-assets/flows/02-guest-cart.mp4",
      durationSec,
      userEmail: "(guest)",
      endpoints,
      persistenceConfirmed: persistence,
      status: persistence ? "real backend" : "not recorded",
      shown: `PDP/каталог → додати → діалог/badge → кошик → qty 2 (${productSlug || "product"})`,
    });
  });

  test("03-cart-merge", async ({ recordClip, request }) => {
    const credentials = await getVerifiedTestCredentials();
    let persistence = false;
    let endpoints: string[] = [];

    const tokens = await loginUserViaApi(credentials.email, credentials.password);
    await clearAuthenticatedCart(request, tokens.accessToken);

    const { durationSec } = await recordClip(
      path.join(ASSETS.flows, "03-cart-merge.mp4"),
      async (page) => {
        await clearAuthState(page);
        const proof = createApiProofCollector(page);
        const catalogCart = new CatalogCartActions(page);
        const cart = new CartPage(page);

        const product = await catalogCart.addFirstVisibleCatalogProduct();
        await catalogCart.dismissAddedDialogContinueShopping();
        await cart.expectHeaderCount(1);
        await hold(page, 2000);

        await loginViaUi(page, credentials);
        await hold(page, 2000);

        await expect
          .poll(async () => page.evaluate(() => window.localStorage.getItem("guestCart")), {
            timeout: 15_000,
          })
          .toBeNull();

        await cart.expectHeaderCount(1);
        await cart.goto();
        await cart.expectProductInCart(product, 1);
        await hold(page, 3000);

        const serverCount = await getAuthenticatedCartItemCount(
          request,
          (await page.evaluate(() => window.localStorage.getItem("accessToken")))!,
        );
        persistence = serverCount >= 1;
        const summary = proof.summarize("03-cart-merge", persistence);
        endpoints = summary.endpoints;
        proofRows.push(summary);
      },
    );

    flowMetas.push({
      id: "03-cart-merge",
      title: "Login і merge гостьового кошика",
      path: "presentation-assets/flows/03-cart-merge.mp4",
      durationSec,
      userEmail: credentials.email,
      endpoints,
      persistenceConfirmed: persistence,
      status: persistence ? "real backend" : "not recorded",
      shown: "Guest add → login → guestCart очищено → серверний кошик з позицією",
    });
  });

  test("04-checkout", async ({ recordClip, request }) => {
    const credentials = await getVerifiedTestCredentials();
    let persistence = false;
    let endpoints: string[] = [];

    const { durationSec } = await recordClip(
      path.join(ASSETS.flows, "04-checkout.mp4"),
      async (page) => {
        await clearAuthState(page);
        const proof = createApiProofCollector(page);
        await loginViaUi(page, credentials);
        const accessToken = (await page.evaluate(() =>
          window.localStorage.getItem("accessToken"),
        ))!;
        await seedAuthenticatedCart(request, accessToken);

        const checkout = new CheckoutPage(page);
        await checkout.goto();
        await checkout.expectLoaded();
        await hold(page, 2000);

        await checkout.clearContactFields();
        await checkout.submit();
        await checkout.expectValidationError(/обов'язкові контактні дані/i);
        await hold(page, 2500);

        await checkout.fillContacts();
        await checkout.fillUkrPoshtaDelivery();
        await checkout.selectCashPayment();
        await hold(page, 2000);
        await checkout.submit();
        await checkout.expectOrderCreated();
        await hold(page, 3000);

        await expect
          .poll(async () => getAuthenticatedCartItemCount(request, accessToken), {
            timeout: 15_000,
          })
          .toBe(0);

        persistence = proof.rows.some(
          (row) =>
            row.url.includes("/me/cart/checkout") &&
            row.method === "POST" &&
            row.status >= 200 &&
            row.status < 300,
        );
        const summary = proof.summarize("04-checkout", persistence);
        endpoints = summary.endpoints;
        proofRows.push(summary);
      },
    );

    flowMetas.push({
      id: "04-checkout",
      title: "Checkout (validation + cash)",
      path: "presentation-assets/flows/04-checkout.mp4",
      durationSec,
      userEmail: credentials.email,
      endpoints,
      persistenceConfirmed: persistence,
      status: persistence ? "real backend" : "not recorded",
      shown: "Checkout → validation error → контакти/Укрпошта/Cash → успішне замовлення",
    });
  });

  test("05-payment-result", async ({ recordClip, request }) => {
    test.skip(skipIfLiqPaySandboxUnavailable(), "LiqPay sandbox private key is not configured");

    const credentials = await getVerifiedTestCredentials();
    let persistence = false;
    let endpoints: string[] = [];
    let note =
      "Після реального redirect на LiqPay sandbox підписаний webhook доставлено на backend endpoint (localhost callback недоступний з хмари LiqPay).";

    const { durationSec } = await recordClip(
      path.join(ASSETS.flows, "05-payment-result.mp4"),
      async (page) => {
        await clearAuthState(page);
        const proof = createApiProofCollector(page);
        await loginViaUi(page, credentials);
        const accessToken = (await page.evaluate(() =>
          window.localStorage.getItem("accessToken"),
        ))!;
        await seedAuthenticatedCart(request, accessToken);

        const checkout = new CheckoutPage(page);
        const resultPage = new CheckoutResultPage(page);

        await checkout.goto();
        await checkout.expectLoaded();
        await checkout.fillContacts();
        await checkout.fillUkrPoshtaDelivery();
        await checkout.selectCardPayment();
        await hold(page, 1500);

        const checkoutPostPromise = page.waitForResponse(
          (response) =>
            response.request().method() === "POST" &&
            response.url().includes("/me/cart/checkout"),
          { timeout: 45_000 },
        );

        await checkout.submit();
        const checkoutResponse = await checkoutPostPromise;
        expect(checkoutResponse.status()).toBeGreaterThanOrEqual(200);
        expect(checkoutResponse.status()).toBeLessThan(400);

        // LiqPay navigation often drops Network.getResponseBody — resolve order via API.
        await page
          .waitForURL(/liqpay\.ua/i, { timeout: 30_000 })
          .catch(() => undefined);
        if (/liqpay\.ua/i.test(page.url())) {
          await hold(page, 3500);
        } else {
          note =
            "Redirect на LiqPay UI не відбувся вчасно; checkout API та sandbox webhook все одно пройшли через реальний backend.";
        }

        const ordersResponse = await request.get(`${getApiBaseUrl()}/me/orders`, {
          headers: { Authorization: `Bearer ${accessToken}` },
          params: { page: 1, pageSize: 5 },
        });
        expect(ordersResponse.ok()).toBeTruthy();
        const ordersPayload = (await ordersResponse.json()) as {
          items?: Array<{ orderId: number; orderNumber: string }>;
        };
        const latest = ordersPayload.items?.[0];
        expect(latest?.orderNumber).toBeTruthy();

        const detailsResponse = await request.get(
          `${getApiBaseUrl()}/me/orders/${latest!.orderId}`,
          { headers: { Authorization: `Bearer ${accessToken}` } },
        );
        expect(detailsResponse.ok()).toBeTruthy();
        const details = (await detailsResponse.json()) as {
          orderNumber: string;
          payment?: { transactionId?: string | null; status?: string } | null;
        };

        const orderNumber = details.orderNumber || latest!.orderNumber;
        const paymentRef = details.payment?.transactionId || orderNumber;

        await page.goto(`/checkout/result?order_id=${encodeURIComponent(orderNumber)}`);
        await hold(page, 2000);

        // Real sandbox-signed webhook to backend (LiqPay cloud cannot call localhost:8080).
        await postLiqPayWebhook(request, paymentRef, "success");

        await resultPage.expectPaid();
        await hold(page, 3000);

        const status = await getOrderPaymentStatus(request, accessToken, orderNumber);
        persistence = Boolean(status && /completed|paid|success|captured/i.test(status));
        const summary = proof.summarize("05-payment-result", persistence);
        endpoints = summary.endpoints;
        if (!endpoints.some((e) => e.includes("/integrations/liqpay/webhook"))) {
          endpoints = [...endpoints, "POST /integrations/liqpay/webhook"];
        }
        summary.endpoints = endpoints;
        proofRows.push(summary);
      },
    );

    flowMetas.push({
      id: "05-payment-result",
      title: "Card payment (LiqPay sandbox) + result",
      path: "presentation-assets/flows/05-payment-result.mp4",
      durationSec,
      userEmail: credentials.email,
      endpoints,
      persistenceConfirmed: persistence,
      status: persistence ? "real backend" : "not recorded",
      shown: "Checkout Card → LiqPay sandbox → webhook → «Оплачено успішно!»",
      note,
    });
  });

  test("06-review", async ({ recordClip, request }) => {
    test.skip(skipIfLiqPaySandboxUnavailable(), "LiqPay sandbox private key is not configured");

    const buyer = await loginReviewBuyer();
    const product = await prepareReviewProduct(request);
    const profile = await getCurrentUserProfile(request, buyer.accessToken);
    purgeProductReviewsForUser(product.id, profile.id);
    await ensurePaidPurchaseForProduct(request, buyer.accessToken, product);

    let persistence = false;
    let endpoints: string[] = [];
    const comment = createUniqueReviewComment("presentation");

    const { durationSec } = await recordClip(
      path.join(ASSETS.flows, "06-review.mp4"),
      async (page) => {
        await clearAuthState(page);
        const proof = createApiProofCollector(page);
        await loginViaUi(page, {
          email: testUsers.reviewBuyer.email,
          password: testUsers.reviewBuyer.password,
        });

        const reviews = new ProductReviewsPage(page);
        await reviews.gotoProduct(product.slug);
        await hold(page, 2000);
        await reviews.scrollToReviews();
        await hold(page, 2000);

        await reviews.setRating(5);
        await reviews.fillComment(comment);
        await hold(page, 1500);
        await reviews.submit();
        await reviews.expectReviewVisible(comment);
        await hold(page, 3000);

        const apiReviews = await listProductReviews(request, product.id);
        persistence = apiReviews.some((review) => review.comment === comment);
        const summary = proof.summarize("06-review", persistence);
        endpoints = summary.endpoints;
        proofRows.push(summary);
      },
    );

    // Best-effort cleanup so re-runs stay stable.
    try {
      purgeProductReviewsForUser(product.id, profile.id);
    } catch {
      // ignore
    }

    flowMetas.push({
      id: "06-review",
      title: "Створення відгуку після покупки",
      path: "presentation-assets/flows/06-review.mp4",
      durationSec,
      userEmail: testUsers.reviewBuyer.email,
      endpoints,
      persistenceConfirmed: persistence,
      status: persistence ? "real backend" : "not recorded",
      shown: `PDP ${product.slug} → рейтинг 5 + comment → відгук у списку (підтверджено API)`,
    });
  });
});

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
import { presentationLoginOnCurrentPage } from "../helpers/presentation-login";
import { clearAuthState, loginViaUi } from "../../e2e/fixtures/auth.fixture";
import {
  getVerifiedTestCredentials,
  loginUserViaApi,
} from "../../e2e/fixtures/api.helper";
import { getApiBaseUrl } from "../../e2e/fixtures/backend.helper";
import { getCatalogTestData } from "../../e2e/fixtures/catalog.fixture";
import {
  clearAuthenticatedCart,
  getInStockCatalogProduct,
} from "../../e2e/fixtures/cart.fixture";
import {
  getAuthenticatedCartItemCount,
  getOrderPaymentStatus,
  postLiqPayWebhook,
  seedAuthenticatedCart,
  skipIfLiqPaySandboxUnavailable,
} from "../../e2e/fixtures/checkout.fixture";
import {
  clearReviewCreateRateLimit,
  ensurePaidPurchaseForProduct,
  getCurrentUserProfile,
  getLatestPurchasedProduct,
  getUnpurchasedCatalogProduct,
  listProductReviews,
  prepareReviewProduct,
  purgeProductReviewsForUser,
} from "../../e2e/fixtures/reviews.fixture";
import { CatalogPage } from "../../e2e/pages/catalog.page";
import {
  CartPage,
  CatalogCartActions,
  ProductDetailsCartActions,
} from "../../e2e/pages/cart.page";
import { CheckoutPage, CheckoutResultPage } from "../../e2e/pages/checkout.page";
import { ProductReviewsPage } from "../../e2e/pages/product-reviews.page";
import { realisticClick } from "../../e2e/helpers/realistic-pointer";

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

const FLOW_ORDER = [
  "01-catalog",
  "02-guest-cart",
  "03-checkout-payment",
  "04-review",
] as const;

function readExistingProofRows(proofPath: string): ProofRow[] {
  if (!fs.existsSync(proofPath)) {
    return [];
  }

  const rows: ProofRow[] = [];
  for (const line of fs.readFileSync(proofPath, "utf8").split(/\r?\n/)) {
    if (!line.startsWith("| ") || line.startsWith("| Сценарій") || line.startsWith("|---")) {
      continue;
    }
    const cells = line
      .slice(1, -1)
      .split("|")
      .map((cell) => cell.trim());
    if (cells.length < 4 || !FLOW_ORDER.includes(cells[0] as (typeof FLOW_ORDER)[number])) {
      continue;
    }
    rows.push({
      scenario: cells[0],
      endpoints: cells[1] === "—" ? [] : cells[1].split("<br>").filter(Boolean),
      statuses:
        cells[2] === "—"
          ? []
          : cells[2]
              .split(",")
              .map((value) => Number(value.trim()))
              .filter((value) => Number.isFinite(value)),
      result: cells[3],
    });
  }
  return rows;
}

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
    // Merge so re-recording a single scenario keeps other clips' meta/proof.
    const existingMetas: FlowMeta[] = (() => {
      if (!fs.existsSync(MANIFEST_META_PATH)) {
        return [];
      }
      try {
        const parsed = JSON.parse(fs.readFileSync(MANIFEST_META_PATH, "utf8")) as FlowMeta[];
        return Array.isArray(parsed) ? parsed : [];
      } catch {
        return [];
      }
    })();

    const metaById = new Map(existingMetas.map((meta) => [meta.id, meta]));
    for (const meta of flowMetas) {
      metaById.set(meta.id, meta);
    }
    // Keep only current flow ids (drops retired clips like former 03-cart-merge).
    const mergedMetas = FLOW_ORDER.filter((id) => metaById.has(id)).map(
      (id) => metaById.get(id)!,
    );

    const existingProof = readExistingProofRows(PROOF_PATH);
    const proofByScenario = new Map(existingProof.map((row) => [row.scenario, row]));
    // If a prior single-scenario run wiped the proof table, rebuild missing rows from meta.
    for (const meta of mergedMetas) {
      if (!proofByScenario.has(meta.id)) {
        proofByScenario.set(meta.id, {
          scenario: meta.id,
          endpoints: meta.endpoints,
          statuses: [],
          result: meta.persistenceConfirmed ? "real backend confirmed" : "not recorded",
        });
      }
    }
    for (const row of proofRows) {
      proofByScenario.set(row.scenario, row);
    }
    const mergedProof = FLOW_ORDER.filter((id) => proofByScenario.has(id)).map(
      (id) => proofByScenario.get(id)!,
    );

    writeProofReport(PROOF_PATH, mergedProof, {
      "API base": getApiBaseUrl(),
      Frontend: process.env.PLAYWRIGHT_BASE_URL ?? "http://127.0.0.1:3000",
      "Mock routes": "none (page.route not used)",
    });
    fs.writeFileSync(MANIFEST_META_PATH, JSON.stringify(mergedMetas, null, 2), "utf8");
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

        await page.goto("/");
        await hold(page, 2500);

        await catalog.openCategoryFromHeader(
          catalogData.rootCategory.name,
          catalogData.subcategory.name,
          catalogData.subcategory.id,
          1500,
        );
        await hold(page, 2500);

        await catalog.applyFormat("Паперова", "паперовий", 500);
        await hold(page, 2500);
        await catalog.applyMinimumPrice(100, 500);
        await hold(page, 2500);
        await catalog.selectSort("Від найдешевших", "price_asc", 1000);
        await hold(page, 2500);

        await expect(page).toHaveURL(
          new RegExp(`/catalog/${catalogData.subcategory.slug}(?:\\?|$)`),
        );
        await catalog.expectBrowserSearchParams({
          format: "паперовий",
          minPrice: "100",
          sort: "price_asc",
        });
        await catalog.expectSelectedFilter(catalogData.subcategory.name);
        await catalog.expectSelectedFilter("Паперова");

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
      { addressBar: true },
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
      shown: "Головна → каталог у хедері → категорія → формат «Паперова» → ціна від 100 → сортування «Від найдешевших» / URL",
    });
  });

  test("02-guest-cart", async ({ recordClip, request }) => {
    const catalogData = await getCatalogTestData(request);
    const credentials = await getVerifiedTestCredentials();
    const tokens = await loginUserViaApi(credentials.email, credentials.password);
    await clearAuthenticatedCart(request, tokens.accessToken);

    let persistence = false;
    let endpoints: string[] = [];
    let productSlug = "";

    const { durationSec } = await recordClip(
      path.join(ASSETS.flows, "02-guest-cart.mp4"),
      async (page) => {
        await clearAuthState(page);
        const proof = createApiProofCollector(page);
        const catalog = new CatalogPage(page);
        const catalogCart = new CatalogCartActions(page);
        const details = new ProductDetailsCartActions(page);
        const cart = new CartPage(page);

        // Continue from the filtered catalog state where 01-catalog ends.
        await catalog.gotoPresentationEndState(catalogData.subcategory.slug);
        await hold(page, 2500);

        const product = await catalogCart.openRandomVisibleProduct(500);
        productSlug = product.slug;
        await hold(page, 2000);

        await details.setQuantity(3, 450);
        await hold(page, 800);
        await details.addToCart(500);
        await hold(page, 1500);
        await expect(page.getByTestId("header-cart-badge")).toHaveText("3");

        await details.goToCartFromDialog(500);
        await hold(page, 2500);
        await cart.expectProductInCart(product, 3);
        await cart.expectHeaderCount(3);

        await cart.clickCheckoutCta(500);
        await expect(page).toHaveURL(/\/auth\/login\?.*redirect=/);
        await hold(page, 1500);

        await presentationLoginOnCurrentPage(page, credentials, {
          expectPath: /\/checkout/,
        });
        await hold(page, 2500);

        await expect
          .poll(async () => page.evaluate(() => window.localStorage.getItem("guestCart")), {
            timeout: 15_000,
          })
          .toBeNull();

        const accessToken = (await page.evaluate(() =>
          window.localStorage.getItem("accessToken"),
        ))!;
        const serverCount = await getAuthenticatedCartItemCount(request, accessToken);
        persistence = serverCount >= 3;
        const summary = proof.summarize("02-guest-cart", persistence);
        endpoints = summary.endpoints;
        proofRows.push(summary);
      },
    );

    // Retired clip from the former split scenarios.
    const retiredMergeClip = path.join(ASSETS.flows, "03-cart-merge.mp4");
    if (fs.existsSync(retiredMergeClip)) {
      fs.unlinkSync(retiredMergeClip);
    }

    flowMetas.push({
      id: "02-guest-cart",
      title: "Гостьовий кошик і login merge",
      path: "presentation-assets/flows/02-guest-cart.mp4",
      durationSec,
      userEmail: credentials.email,
      endpoints,
      persistenceConfirmed: persistence,
      status: persistence ? "real backend" : "not recorded",
      shown: `Кінець 01 → PDP (${productSlug || "product"}) qty 3 → кошик → оформити → успішний login`,
    });
  });

  test("03-checkout-payment", async ({ recordClip, request }) => {
    test.skip(skipIfLiqPaySandboxUnavailable(), "LiqPay sandbox private key is not configured");

    const credentials = await getVerifiedTestCredentials();
    let persistence = false;
    let endpoints: string[] = [];
    let deliveryNote = "";
    let note =
      "Після реального redirect на LiqPay sandbox підписаний webhook доставлено на backend endpoint (localhost callback недоступний з хмари LiqPay).";

    const { durationSec } = await recordClip(
      path.join(ASSETS.flows, "03-checkout-payment.mp4"),
      async (page) => {
        await clearAuthState(page);
        const proof = createApiProofCollector(page);
        await loginViaUi(page, credentials);
        const accessToken = (await page.evaluate(() =>
          window.localStorage.getItem("accessToken"),
        ))!;
        // Same cart shape as the end of 02-guest-cart (qty 3 after merge).
        await seedAuthenticatedCart(request, accessToken, undefined, 3);

        const cart = new CartPage(page);
        const checkout = new CheckoutPage(page);
        const resultPage = new CheckoutResultPage(page);

        // Recording starts in the cart (continuation after login merge).
        await cart.goto();
        await cart.expectHeaderCount(3);
        await hold(page, 2500);

        await cart.clickCheckoutCta(500);
        await checkout.expectLoaded();
        await hold(page, 1500);

        // First/last name come from the profile — only ensure phone if missing.
        await checkout.awaitPrefillContacts(undefined, 500);
        await hold(page, 600);
        await checkout.fillMiddleName("Іванівна", 500);
        await hold(page, 800);

        const delivery = await checkout.fillNovaPoshtaDelivery({
          cityQuery: "Львів",
          branchFilter: "Поштомат",
          presentationPauseMs: 500,
        });
        deliveryNote = `${delivery.city} / ${delivery.branch}`;
        await hold(page, 1000);

        await checkout.selectCardPayment(500);
        await hold(page, 1000);
        await checkout.selectGiftWrap(500);
        await hold(page, 1500);

        const checkoutPostPromise = page.waitForResponse(
          (response) =>
            response.request().method() === "POST" &&
            response.url().includes("/me/cart/checkout"),
          { timeout: 45_000 },
        );

        await checkout.submit(500);
        const checkoutResponse = await checkoutPostPromise;
        expect(checkoutResponse.status()).toBeGreaterThanOrEqual(200);
        expect(checkoutResponse.status()).toBeLessThan(400);

        // LiqPay navigation often drops Network.getResponseBody — resolve order via API.
        await page.waitForURL(/liqpay\.ua/i, { timeout: 30_000 }).catch(() => undefined);
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
        const summary = proof.summarize("03-checkout-payment", persistence);
        endpoints = summary.endpoints;
        if (!endpoints.some((e) => e.includes("/integrations/liqpay/webhook"))) {
          endpoints = [...endpoints, "POST /integrations/liqpay/webhook"];
        }
        summary.endpoints = endpoints;
        proofRows.push(summary);
      },
    );

    for (const retired of ["04-checkout.mp4", "05-payment-result.mp4"]) {
      const retiredPath = path.join(ASSETS.flows, retired);
      if (fs.existsSync(retiredPath)) {
        fs.unlinkSync(retiredPath);
      }
    }

    flowMetas.push({
      id: "03-checkout-payment",
      title: "Checkout Nova Poshta + card payment",
      path: "presentation-assets/flows/03-checkout-payment.mp4",
      durationSec,
      userEmail: credentials.email,
      endpoints,
      persistenceConfirmed: persistence,
      status: persistence ? "real backend" : "not recorded",
      shown: `Кошик → оформити → по батькові → НП (${deliveryNote || "місто/поштомат"}) → Card → подарункова упаковка → LiqPay → «Оплачено успішно!»`,
      note,
    });
  });

  test("04-review", async ({ recordClip, request }) => {
    test.skip(skipIfLiqPaySandboxUnavailable(), "LiqPay sandbox private key is not configured");

    const credentials = await getVerifiedTestCredentials();
    const tokens = await loginUserViaApi(credentials.email, credentials.password);
    const profile = await getCurrentUserProfile(request, tokens.accessToken);

    // Prefer the book purchased in scenario 03; seed a paid purchase if missing.
    let purchased = await getLatestPurchasedProduct(request, tokens.accessToken);
    if (!purchased) {
      purchased = await prepareReviewProduct(request);
      await ensurePaidPurchaseForProduct(request, tokens.accessToken, purchased);
    }
    purgeProductReviewsForUser(purchased.id, profile.id);

    // A book the buyer has never purchased — for guest lock + "verified purchase required".
    const guestBrowseProduct = await getUnpurchasedCatalogProduct(
      request,
      tokens.accessToken,
      [purchased.id],
    );
    const guestSlug = guestBrowseProduct.slug;
    purgeProductReviewsForUser(guestBrowseProduct.id, profile.id);

    let persistence = false;
    let endpoints: string[] = [];
    const comment =
      "Чудова книга — легкий стиль і сильна атмосфера. Читала на одному диханні, щиро рекомендую!";

    const { durationSec } = await recordClip(
      path.join(ASSETS.flows, "04-review.mp4"),
      async (page) => {
        await clearAuthState(page);
        const proof = createApiProofCollector(page);
        const reviews = new ProductReviewsPage(page);

        // Guest tries to leave a review on some book — form is locked (keep open brisk).
        await reviews.gotoProduct(guestSlug);
        await hold(page, 400);
        await reviews.scrollToReviews(300);
        await hold(page, 200);
        await reviews.demonstrateGuestBlocked(300, { highlightMs: 900 });
        await hold(page, 250);

        const profileLink = page.getByRole("link", { name: /Профіль/ });
        await realisticClick(page, profileLink, { hoverMs: 350 });
        await page.waitForURL(/\/auth\/login|\/me/, { timeout: 30_000 });
        if (!/\/auth\/login/.test(page.url())) {
          await page.goto("/auth/login");
        }
        await hold(page, 500);

        await presentationLoginOnCurrentPage(page, credentials);
        await hold(page, 900);

        // Logged in, but this book was not purchased — review must be rejected.
        await reviews.gotoProduct(guestSlug);
        await hold(page, 700);
        await reviews.scrollToReviews(300);
        await hold(page, 400);
        await expect(reviews.formTitle).toHaveText("Залишити відгук");
        await reviews.setRating(5, 350);
        await hold(page, 300);
        await reviews.fillComment("Спроба відгуку без покупки", 350);
        await hold(page, 400);
        await reviews.submit(350);
        await reviews.expectPurchaseRequiredError();
        await hold(page, 1800);

        // Purchased book from the previous scenario — review succeeds.
        clearReviewCreateRateLimit(profile.id, purchased!.id);
        await reviews.gotoProduct(purchased!.slug);
        await hold(page, 800);
        await reviews.scrollToReviews(300);
        await hold(page, 600);

        await expect(reviews.formTitle).toHaveText("Залишити відгук");
        await reviews.setRating(5, 350);
        await hold(page, 350);
        await reviews.fillComment(comment, 400);
        await hold(page, 800);
        await reviews.submit(350);
        await reviews.expectReviewVisible(comment);
        await hold(page, 2500);

        const apiReviews = await listProductReviews(request, purchased!.id);
        persistence = apiReviews.some((review) => review.comment === comment);
        const summary = proof.summarize("04-review", persistence);
        endpoints = summary.endpoints;
        proofRows.push(summary);
      },
    );

    try {
      purgeProductReviewsForUser(purchased.id, profile.id);
    } catch {
      // ignore
    }

    for (const retired of ["06-review.mp4"]) {
      const retiredPath = path.join(ASSETS.flows, retired);
      if (fs.existsSync(retiredPath)) {
        fs.unlinkSync(retiredPath);
      }
    }

    flowMetas.push({
      id: "04-review",
      title: "Відгук після покупки",
      path: "presentation-assets/flows/04-review.mp4",
      durationSec,
      userEmail: credentials.email,
      endpoints,
      persistenceConfirmed: persistence,
      status: persistence ? "real backend" : "not recorded",
      shown: `Guest locked → login → відгук без покупки (помилка) → PDP ${purchased.slug} → успішний відгук`,
    });
  });
});

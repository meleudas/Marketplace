import { expect, test } from "@playwright/test";
import { clearAuthState, loginViaUi } from "../fixtures/auth.fixture";
import { getVerifiedTestCredentials } from "../fixtures/api.helper";
import { skipIfBackendAuthUnavailable } from "../fixtures/backend.helper";
import { CatalogCartActions, CartPage } from "../pages/cart.page";
import { CheckoutPage, CheckoutResultPage } from "../pages/checkout.page";
import {
  getAuthenticatedCartItemCount,
  getOrderPaymentStatus,
  installCheckoutPaymentIntercept,
  postLiqPayWebhook,
  seedAuthenticatedCart,
  skipIfLiqPaySandboxUnavailable,
} from "../fixtures/checkout.fixture";

async function getAccessToken(page: import("@playwright/test").Page): Promise<string> {
  const token = await page.evaluate(() => window.localStorage.getItem("accessToken"));
  expect(token).toBeTruthy();
  return token!;
}

test.describe("Checkout access - guest", () => {
  test.beforeEach(async ({ page }) => {
    await clearAuthState(page);
  });

  test("guest cannot open checkout and is redirected to login", async ({ page }) => {
    await page.goto("/checkout");
    await expect(page).toHaveURL(/\/auth\/login/, { timeout: 15_000 });
    expect(page.url()).toMatch(/redirect=%2Fcheckout|redirect=\/checkout/);
    await expect(page.getByRole("heading", { name: "Вхід" })).toBeVisible();
  });

  test("guest cart checkout CTA goes to login instead of payment", async ({ page }) => {
    const catalogCart = new CatalogCartActions(page);
    await catalogCart.addFirstVisibleCatalogProduct();
    await catalogCart.goToCartFromDialog();

    const cta = page.getByTestId("cart-checkout-cta");
    await expect(cta).toBeVisible();
    await expect(cta).toHaveAttribute("href", /\/auth\/login\?redirect=\/checkout/);

    await cta.click();
    await expect(page).toHaveURL(/\/auth\/login/, { timeout: 15_000 });
    expect(page.url()).toMatch(/redirect=%2Fcheckout|redirect=\/checkout/);
    await expect(page.getByRole("heading", { name: "Вхід" })).toBeVisible();
  });
});

test.describe("Checkout and payment - authenticated", () => {
  test.beforeEach(async ({ page, request }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    await clearAuthState(page);
    const credentials = await getVerifiedTestCredentials();
    await loginViaUi(page, credentials);

    const accessToken = await getAccessToken(page);
    await seedAuthenticatedCart(request, accessToken);
  });

  test("authenticated user can open checkout from cart", async ({ page }) => {
    const cart = new CartPage(page);
    const checkout = new CheckoutPage(page);

    await cart.goto();
    await expect(page.getByTestId("cart-checkout-cta")).toBeVisible();
    await page.getByTestId("cart-checkout-cta").click();

    await expect(page).toHaveURL(/\/checkout/);
    await checkout.expectLoaded();
  });

  test("shows validation errors for required checkout fields", async ({ page }) => {
    const checkout = new CheckoutPage(page);
    await checkout.goto();
    await checkout.expectLoaded();

    await checkout.clearContactFields();
    await checkout.submit();
    await checkout.expectValidationError(/обов'язкові контактні дані/i);

    await checkout.fillContacts();
    await checkout.submit();
    await checkout.expectValidationError(/місто та відділення|Укрпошти|адресні поля/i);

    await checkout.fillUkrPoshtaDelivery({
      city: "",
      postalCode: "",
      branch: "",
    });
    await checkout.submit();
    await checkout.expectValidationError(/Укрпошти/i);
  });

  test("creates an order with cash-on-delivery and clears the cart", async ({
    page,
    request,
  }) => {
    const checkout = new CheckoutPage(page);
    const cart = new CartPage(page);

    await checkout.goto();
    await checkout.expectLoaded();
    await checkout.fillContacts();
    await checkout.fillUkrPoshtaDelivery();
    await checkout.selectCashPayment();
    await checkout.submit();
    await checkout.expectOrderCreated();

    const accessToken = await getAccessToken(page);
    await expect
      .poll(async () => getAuthenticatedCartItemCount(request, accessToken), {
        timeout: 15_000,
      })
      .toBe(0);

    await cart.goto();
    await expect(page.getByRole("heading", { name: "Кошик порожній" })).toBeVisible();
    await expect(page.getByTestId("header-cart-badge")).toHaveCount(0);
  });

  test("successful sandbox payment marks order paid, clears cart, and shows confirmation", async ({
    page,
    request,
  }) => {
    test.skip(skipIfLiqPaySandboxUnavailable(), "LiqPay sandbox private key is not configured");

    const checkout = new CheckoutPage(page);
    const resultPage = new CheckoutResultPage(page);
    const intercept = await installCheckoutPaymentIntercept(page);

    await checkout.goto();
    await checkout.expectLoaded();
    await checkout.fillContacts();
    await checkout.fillUkrPoshtaDelivery();
    await checkout.selectCardPayment();

    await Promise.all([
      page.waitForURL(/\/checkout\/result\?order_id=/, { timeout: 45_000 }),
      checkout.submit(),
    ]);

    const created = intercept.getLastCheckout()?.createdOrders?.[0];
    expect(created?.orderNumber).toBeTruthy();

    const paymentRef = created!.payment?.transactionId || created!.orderNumber;
    await postLiqPayWebhook(request, paymentRef, "success");

    await resultPage.expectPaid();

    const accessToken = await getAccessToken(page);
    await expect
      .poll(async () => getOrderPaymentStatus(request, accessToken, created!.orderNumber), {
        timeout: 20_000,
      })
      .toMatch(/completed|paid|success|captured/i);

    await expect
      .poll(async () => getAuthenticatedCartItemCount(request, accessToken), {
        timeout: 15_000,
      })
      .toBe(0);

    await page.goto("/cart");
    await expect(page.getByRole("heading", { name: "Кошик порожній" })).toBeVisible();
  });

  test("failed sandbox payment does not mark the order as paid", async ({ page, request }) => {
    test.skip(skipIfLiqPaySandboxUnavailable(), "LiqPay sandbox private key is not configured");

    const checkout = new CheckoutPage(page);
    const resultPage = new CheckoutResultPage(page);
    const intercept = await installCheckoutPaymentIntercept(page);

    await checkout.goto();
    await checkout.expectLoaded();
    await checkout.fillContacts();
    await checkout.fillUkrPoshtaDelivery();
    await checkout.selectCardPayment();

    await Promise.all([
      page.waitForURL(/\/checkout\/result\?order_id=/, { timeout: 45_000 }),
      checkout.submit(),
    ]);

    const created = intercept.getLastCheckout()?.createdOrders?.[0];
    expect(created?.orderNumber).toBeTruthy();

    const paymentRef = created!.payment?.transactionId || created!.orderNumber;
    await postLiqPayWebhook(request, paymentRef, "failure");

    await resultPage.expectFailed();

    const accessToken = await getAccessToken(page);
    const status = await getOrderPaymentStatus(request, accessToken, created!.orderNumber);
    expect(status).toBeTruthy();
    expect(String(status)).toMatch(/failed|failure|pending/i);
    expect(String(status)).not.toMatch(/completed|paid|success|captured/i);
  });

  test("cancelled payment leaves the order unpaid", async ({ page, request }) => {
    test.skip(skipIfLiqPaySandboxUnavailable(), "LiqPay sandbox private key is not configured");

    const checkout = new CheckoutPage(page);
    const resultPage = new CheckoutResultPage(page);
    const intercept = await installCheckoutPaymentIntercept(page);

    await checkout.goto();
    await checkout.expectLoaded();
    await checkout.fillContacts();
    await checkout.fillUkrPoshtaDelivery();
    await checkout.selectCardPayment();

    await Promise.all([
      page.waitForURL(/\/checkout\/result\?order_id=/, { timeout: 45_000 }),
      checkout.submit(),
    ]);

    const created = intercept.getLastCheckout()?.createdOrders?.[0];
    expect(created?.orderNumber).toBeTruthy();

    // No webhook = user cancelled / abandoned LiqPay checkout.
    await resultPage.expectPending();

    const accessToken = await getAccessToken(page);
    const status = await getOrderPaymentStatus(request, accessToken, created!.orderNumber);
    expect(String(status ?? "Pending")).not.toMatch(/completed|paid|success|captured/i);
  });
});

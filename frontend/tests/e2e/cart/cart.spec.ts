import { expect, test } from "@playwright/test";
import { clearAuthState, loginViaUi } from "../fixtures/auth.fixture";
import { getVerifiedTestCredentials } from "../fixtures/api.helper";
import { skipIfBackendAuthUnavailable } from "../fixtures/backend.helper";
import {
  clearAuthenticatedCart,
  getInStockCatalogProduct,
  type CartProductFixture,
} from "../fixtures/cart.fixture";
import {
  CartPage,
  CatalogCartActions,
  ProductDetailsCartActions,
} from "../pages/cart.page";

test.describe("Add to cart from catalog card", () => {
  test.beforeEach(async ({ page }) => {
    await clearAuthState(page);
  });

  test("adds a product from the catalog card and keeps it across navigation", async ({
    page,
  }) => {
    const catalogCart = new CatalogCartActions(page);
    const cart = new CartPage(page);

    const product = await catalogCart.addFirstVisibleCatalogProduct();
    await cart.expectHeaderCount(1);
    await catalogCart.goToCartFromDialog();
    await cart.expectProductInCart(product, 1);

    await page.goto("/catalog");
    await expect(page.getByRole("heading", { name: "Каталог", exact: true })).toBeVisible();
    await cart.expectHeaderCount(1);

    await cart.goto();
    await cart.expectProductInCart(product, 1);
  });

  test("increments quantity when the same catalog product is added again", async ({ page }) => {
    const catalogCart = new CatalogCartActions(page);
    const cart = new CartPage(page);

    const product = await catalogCart.addFirstVisibleCatalogProduct();
    await catalogCart.dismissAddedDialogContinueShopping();
    await cart.expectHeaderCount(1);

    // Stay on the same catalog page so the card remains available.
    await page
      .locator(`[data-testid="product-card-add-to-cart"][data-product-id="${product.id}"]`)
      .click();
    await expect(page.getByRole("dialog", { name: "Додано до кошика" })).toBeVisible();
    await cart.expectHeaderCount(2);
    await catalogCart.goToCartFromDialog();
    await cart.expectProductInCart(product, 2);
  });
});

test.describe("Add to cart from product details", () => {
  let product: CartProductFixture;

  test.beforeAll(async ({ request }) => {
    product = await getInStockCatalogProduct(request);
  });

  test.beforeEach(async ({ page, request }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    await clearAuthState(page);
    const credentials = await getVerifiedTestCredentials();
    await loginViaUi(page, credentials);

    const accessToken = await page.evaluate(() => window.localStorage.getItem("accessToken"));
    expect(accessToken).toBeTruthy();
    await clearAuthenticatedCart(request, accessToken!);
    await page.goto("/");
    await expect(page.getByTestId("header-cart-badge")).toHaveCount(0);
  });

  test("adds a product from the details page and keeps it across navigation", async ({
    page,
  }) => {
    const details = new ProductDetailsCartActions(page);
    const cart = new CartPage(page);

    await details.gotoProduct(product.slug);
    await details.addToCart();
    await cart.expectHeaderCount(1);
    await details.dismissAddedDialog();

    await cart.goto();
    await cart.expectProductInCart(product, 1);

    await page.goto(`/products/${product.slug}`);
    await expect(page.getByTestId("product-details-add-to-cart")).toBeVisible();
    await cart.expectHeaderCount(1);

    await cart.goto();
    await cart.expectProductInCart(product, 1);
  });

  test("increments quantity when the same product is added again from details", async ({
    page,
  }) => {
    const details = new ProductDetailsCartActions(page);
    const cart = new CartPage(page);

    await details.gotoProduct(product.slug);
    await details.addToCart();
    await cart.expectHeaderCount(1);
    await details.dismissAddedDialog();

    await details.addToCart();
    await cart.expectHeaderCount(2);
    await details.dismissAddedDialog();

    await cart.goto();
    await cart.expectProductInCart(product, 2);
  });
});

test.describe("Guest cart merge on login", () => {
  test.beforeEach(async ({ page, request }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    // Clear the account cart first, then return to a guest session.
    await clearAuthState(page);
    const credentials = await getVerifiedTestCredentials();
    await loginViaUi(page, credentials);

    const accessToken = await page.evaluate(() => window.localStorage.getItem("accessToken"));
    expect(accessToken).toBeTruthy();
    await clearAuthenticatedCart(request, accessToken!);
    await clearAuthState(page);
  });

  test("keeps guest cart items after the user logs in", async ({ page }) => {
    const catalogCart = new CatalogCartActions(page);
    const cart = new CartPage(page);

    const product = await catalogCart.addFirstVisibleCatalogProduct();
    await catalogCart.dismissAddedDialogContinueShopping();
    await cart.expectHeaderCount(1);

    const guestCartBeforeLogin = await page.evaluate(() => window.localStorage.getItem("guestCart"));
    expect(guestCartBeforeLogin).toBeTruthy();

    const credentials = await getVerifiedTestCredentials();
    await loginViaUi(page, credentials);

    await expect
      .poll(async () => page.evaluate(() => window.localStorage.getItem("guestCart")), {
        timeout: 15_000,
      })
      .toBeNull();

    await cart.expectHeaderCount(1);
    await cart.goto();
    await cart.expectProductInCart(product, 1);
  });
});

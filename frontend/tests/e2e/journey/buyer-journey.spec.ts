import { expect, test } from "@playwright/test";
import { skipIfBackendAuthUnavailable } from "../fixtures/backend.helper";
import { getCatalogTestData, type CatalogTestData } from "../fixtures/catalog.fixture";
import {
  checkoutTestAddress,
  getOrderPaymentStatus,
  installCheckoutPaymentIntercept,
  postLiqPayWebhook,
  skipIfLiqPaySandboxUnavailable,
} from "../fixtures/checkout.fixture";
import {
  createJourneyUser,
  formatExpectedCartTotal,
  registerAndLoginJourneyUser,
} from "../fixtures/journey.fixture";
import {
  createUniqueReviewComment,
  getCurrentUserProfile,
  purgeProductReviewsForUser,
} from "../fixtures/reviews.fixture";
import type { CartProductFixture } from "../fixtures/cart.fixture";
import { CatalogPage } from "../pages/catalog.page";
import { CartPage, CatalogCartActions, ProductDetailsCartActions } from "../pages/cart.page";
import { CheckoutPage, CheckoutResultPage } from "../pages/checkout.page";
import { ProductReviewsPage } from "../pages/product-reviews.page";

test.describe("Full buyer journey", () => {
  test("registers, shops, pays, and leaves a review", async ({ page, request }) => {
    test.setTimeout(180_000);
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");
    test.skip(skipIfLiqPaySandboxUnavailable(), "LiqPay sandbox private key is not configured");

    const catalogData: CatalogTestData = await getCatalogTestData(request);
    const journeyUser = createJourneyUser();

    // 1–2. Open site, register with unique data, confirm email if required, sign in.
    await registerAndLoginJourneyUser(page, journeyUser);
    const accessToken = await page.evaluate(() => window.localStorage.getItem("accessToken"));
    expect(accessToken).toBeTruthy();
    const profile = await getCurrentUserProfile(request, accessToken!);

    // 3–4. Catalog via header → category + format filter.
    const catalog = new CatalogPage(page);
    await catalog.openCategoryFromHeader(
      catalogData.rootCategory.name,
      catalogData.subcategory.name,
      catalogData.subcategory.id,
    );
    await catalog.expectSelectedFilter(catalogData.subcategory.name);
    await catalog.applyFormat("Паперова", "паперовий");
    await catalog.expectSelectedFilter("Паперова");
    await expect(page.getByTestId("product-card-add-to-cart").nth(1)).toBeVisible({
      timeout: 30_000,
    });

    // 5. Add several books from catalog cards.
    const catalogCart = new CatalogCartActions(page);
    const firstBook = await catalogCart.addVisibleCatalogProductAt(0);
    await catalogCart.dismissAddedDialogContinueShopping();
    const secondBook = await catalogCart.addVisibleCatalogProductAt(1);
    await catalogCart.dismissAddedDialogContinueShopping();
    expect(firstBook.id).not.toBe(secondBook.id);

    // 6–7. Open another book details page and add from PDP.
    const otherAddButton = page.locator(
      `[data-testid="product-card-add-to-cart"]:not([data-product-id="${firstBook.id}"]):not([data-product-id="${secondBook.id}"])`,
    );
    await expect(otherAddButton.first()).toBeVisible({ timeout: 30_000 });
    const pdpProductId = Number(await otherAddButton.first().getAttribute("data-product-id"));
    expect(Number.isFinite(pdpProductId)).toBeTruthy();

    const pdpCard = page.locator("article").filter({
      has: page.locator(
        `[data-testid="product-card-add-to-cart"][data-product-id="${pdpProductId}"]`,
      ),
    });
    const pdpHref = await pdpCard.locator('a[href^="/products/"]').first().getAttribute("href");
    const pdpSlug = (pdpHref ?? "").replace(/^\/products\//, "").split("?")[0];
    expect(pdpSlug).toBeTruthy();

    const details = new ProductDetailsCartActions(page);
    await details.gotoProduct(pdpSlug);
    await details.addToCart();

    const addedDialog = page.getByRole("dialog", { name: "Додано до кошика" });
    const pdpName = (
      await addedDialog.getByTestId("add-to-cart-dialog-product-title").innerText()
    ).trim();
    const pdpPriceText = (
      await addedDialog.getByTestId("add-to-cart-dialog-product-price").innerText()
    ).trim();
    const pdpPrice = Number(pdpPriceText.replace(/[^\d]/g, ""));
    expect(pdpName).toBeTruthy();
    expect(pdpPrice).toBeGreaterThan(0);
    await details.dismissAddedDialog();

    const thirdBook: CartProductFixture = {
      id: pdpProductId,
      name: pdpName,
      slug: pdpSlug,
      price: pdpPrice,
    };

    // 8. Cart: items, quantities, total.
    const cart = new CartPage(page);
    await cart.goto();
    await cart.expectHeaderCount(3);
    await cart.expectProductInCart(firstBook, 1);
    await cart.expectProductInCart(secondBook, 1);
    await cart.expectProductInCart(thirdBook, 1);
    await cart.expectTotal(
      formatExpectedCartTotal([firstBook.price, secondBook.price, thirdBook.price]),
    );

    // 9–11. Checkout → fill → successful sandbox payment → confirmation + empty cart.
    await page.getByTestId("cart-checkout-cta").click();
    const checkout = new CheckoutPage(page);
    await checkout.expectLoaded();
    // Profile hydration can overwrite contacts; fill after email is populated from /users/me.
    await expect(page.locator('input[type="email"]')).toHaveValue(journeyUser.email, {
      timeout: 15_000,
    });
    await checkout.fillContacts({
      firstName: checkoutTestAddress.firstName,
      lastName: checkoutTestAddress.lastName,
      phone: checkoutTestAddress.phone,
    });
    await checkout.fillUkrPoshtaDelivery(checkoutTestAddress);
    await checkout.selectCardPayment();

    const intercept = await installCheckoutPaymentIntercept(page);
    await Promise.all([
      page.waitForURL(/\/checkout\/result\?order_id=/, { timeout: 60_000 }),
      checkout.submit(),
    ]);

    const createdOrders = intercept.getLastCheckout()?.createdOrders ?? [];
    expect(createdOrders.length).toBeGreaterThan(0);

    // Multi-seller checkout may create several orders — pay every one so any purchased
    // book is eligible for a review.
    for (const order of createdOrders) {
      const paymentRef = order.payment?.transactionId || order.orderNumber;
      await postLiqPayWebhook(request, paymentRef, "success");
      await expect
        .poll(
          async () => getOrderPaymentStatus(request, accessToken!, order.orderNumber),
          { timeout: 20_000 },
        )
        .toMatch(/completed|paid|success|captured/i);
    }

    const resultPage = new CheckoutResultPage(page);
    await resultPage.expectPaid();

    await cart.goto();
    await expect(page.getByRole("heading", { name: "Кошик порожній" })).toBeVisible();
    await expect(page.getByTestId("header-cart-badge")).toHaveCount(0);

    // 12–13. Review on a purchased book.
    const reviewBook = firstBook;
    purgeProductReviewsForUser(reviewBook.id, profile.id);
    const reviews = new ProductReviewsPage(page);
    await reviews.gotoProduct(reviewBook.slug);
    await reviews.scrollToReviews();

    const comment = createUniqueReviewComment("journey-review");
    await reviews.setRating(5);
    await reviews.fillComment(comment);
    await reviews.submit();
    await expect(reviews.errorAlert).toHaveCount(0);

    const reviewItem = await reviews.expectReviewVisible(comment);
    await expect(reviewItem.getByTestId("product-review-author")).toBeVisible();
    await expect(reviewItem.getByTestId("product-review-date")).toBeVisible();
    await expect(reviewItem.getByTestId("product-review-text")).toHaveText(comment);

    purgeProductReviewsForUser(reviewBook.id, profile.id);
  });
});

import { expect, test, type Page } from "@playwright/test";
import { clearAuthState, loginViaUi } from "../fixtures/auth.fixture";
import { skipIfBackendAuthUnavailable } from "../fixtures/backend.helper";
import { skipIfLiqPaySandboxUnavailable } from "../fixtures/checkout.fixture";
import type { CartProductFixture } from "../fixtures/cart.fixture";
import {
  createUniqueReviewComment,
  ensurePaidPurchaseForProduct,
  formatExpectedReviewDate,
  getCurrentUserProfile,
  listProductReviews,
  loginReviewBuyer,
  loginReviewNonBuyer,
  prepareReviewProduct,
  purgeProductReviewsForUser,
} from "../fixtures/reviews.fixture";
import { ProductReviewsPage } from "../pages/product-reviews.page";

async function openAuthenticatedSession(page: Page, accessToken: string): Promise<void> {
  await page.goto("/");
  await page.evaluate((token) => {
    window.localStorage.setItem("accessToken", token);
  }, accessToken);
  await page.reload();
  await expect(page.getByRole("link", { name: /Профіль/ })).toBeVisible({ timeout: 20_000 });
}

test.describe.serial("Product reviews on /products/:slug", () => {
  let product: CartProductFixture;
  let buyerAccessToken = "";
  let buyerUserId = "";
  let nonBuyerAccessToken = "";
  let nonBuyerEmail = "";
  let nonBuyerPassword = "";
  let createdComment = "";

  test.beforeAll(async ({ request }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");
    test.skip(skipIfLiqPaySandboxUnavailable(), "LiqPay sandbox private key is not configured");

    product = await prepareReviewProduct(request);

    const buyer = await loginReviewBuyer();
    buyerAccessToken = buyer.accessToken;
    const buyerProfile = await getCurrentUserProfile(request, buyer.accessToken);
    buyerUserId = buyerProfile.id;

    purgeProductReviewsForUser(product.id, buyerUserId);
    await ensurePaidPurchaseForProduct(request, buyer.accessToken, product);

    const nonBuyer = await loginReviewNonBuyer();
    nonBuyerAccessToken = nonBuyer.accessToken;
    nonBuyerEmail = nonBuyer.email;
    nonBuyerPassword = nonBuyer.password;
    const nonBuyerProfile = await getCurrentUserProfile(request, nonBuyer.accessToken);
    purgeProductReviewsForUser(product.id, nonBuyerProfile.id);
  });

  test.afterAll(() => {
    if (buyerUserId && product) {
      try {
        purgeProductReviewsForUser(product.id, buyerUserId);
      } catch {
        // best-effort cleanup
      }
    }
  });

  test.beforeEach(async ({ page }) => {
    await clearAuthState(page);
  });

  test("guest cannot leave a review", async ({ page }) => {
    const reviews = new ProductReviewsPage(page);
    await reviews.gotoProduct(product.slug);
    await reviews.scrollToReviews();
    await reviews.expectGuestFormLocked();
  });

  test("registered user without purchase cannot leave a review", async ({ page }) => {
    try {
      await openAuthenticatedSession(page, nonBuyerAccessToken);
    } catch {
      await loginViaUi(page, { email: nonBuyerEmail, password: nonBuyerPassword });
    }

    const reviews = new ProductReviewsPage(page);
    await reviews.gotoProduct(product.slug);
    await reviews.scrollToReviews();

    await expect(reviews.formTitle).toHaveText("Залишити відгук");
    await expect(reviews.commentInput).toBeEnabled();

    await reviews.setRating(5);
    await reviews.fillComment(createUniqueReviewComment("no-purchase"));
    await reviews.submit();

    await expect(reviews.errorAlert).toBeVisible({ timeout: 15_000 });
    await expect(reviews.errorAlert).toHaveText(/Не вдалося надіслати відгук/i);
  });

  test("buyer can leave a review and sees text, author, and date", async ({ page, request }) => {
    await openAuthenticatedSession(page, buyerAccessToken);
    const profile = await getCurrentUserProfile(request, buyerAccessToken);

    createdComment = createUniqueReviewComment("purchased");
    const reviews = new ProductReviewsPage(page);
    await reviews.gotoProduct(product.slug);
    await reviews.scrollToReviews();

    await reviews.setRating(5);
    await reviews.fillComment(createdComment);
    await reviews.submit();

    const item = await reviews.expectReviewVisible(createdComment);
    await expect(item.getByTestId("product-review-author")).toHaveText(profile.id);

    const apiReviews = await listProductReviews(request, product.id);
    const created = apiReviews.find((review) => review.comment === createdComment);
    expect(created).toBeTruthy();

    await expect(item.getByTestId("product-review-date")).toHaveText(
      formatExpectedReviewDate(created!.createdAt),
    );
    await expect(item.getByTestId("product-review-text")).toHaveText(createdComment);
  });

  test("empty comment is not submitted", async ({ page }) => {
    await openAuthenticatedSession(page, buyerAccessToken);

    const reviews = new ProductReviewsPage(page);
    await reviews.gotoProduct(product.slug);
    await reviews.scrollToReviews();

    await reviews.setRating(4);
    await reviews.fillComment("   ");
    await reviews.submit();

    await expect(reviews.errorAlert).toBeVisible();
    await expect(reviews.errorAlert).toHaveText(/напишіть відгук/i);
  });

  test("resubmitting does not create duplicate reviews", async ({ page, request }) => {
    await openAuthenticatedSession(page, buyerAccessToken);
    const profile = await getCurrentUserProfile(request, buyerAccessToken);

    const reviews = new ProductReviewsPage(page);
    await reviews.gotoProduct(product.slug);
    await reviews.scrollToReviews();

    expect(createdComment).toBeTruthy();
    await reviews.expectReviewVisible(createdComment);

    const before = await listProductReviews(request, product.id);
    expect(before.filter((review) => review.userId === profile.id)).toHaveLength(1);

    await reviews.setRating(5);
    await reviews.fillComment(createUniqueReviewComment("second-attempt"));
    await reviews.submit();
    await expect(reviews.errorAlert).toBeVisible({ timeout: 15_000 });
    await expect(reviews.errorAlert).toHaveText(/Не вдалося надіслати відгук/i);

    const after = await listProductReviews(request, product.id);
    expect(after.filter((review) => review.userId === profile.id)).toHaveLength(1);
    await reviews.expectReviewCount(createdComment, 1);
  });
});

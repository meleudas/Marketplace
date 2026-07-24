import { expect, type Locator, type Page } from "@playwright/test";

export class ProductReviewsPage {
  readonly page: Page;
  readonly section: Locator;
  readonly form: Locator;
  readonly formTitle: Locator;
  readonly commentInput: Locator;
  readonly submitButton: Locator;
  readonly errorAlert: Locator;
  readonly list: Locator;

  constructor(page: Page) {
    this.page = page;
    this.section = page.getByTestId("product-reviews");
    this.form = page.getByTestId("product-review-form");
    this.formTitle = page.getByTestId("product-review-form-title");
    this.commentInput = page.getByTestId("product-review-comment");
    this.submitButton = page.getByTestId("product-review-submit");
    this.errorAlert = page.getByTestId("product-review-error");
    this.list = page.getByTestId("product-reviews-list");
  }

  async gotoProduct(slug: string): Promise<void> {
    await this.page.goto(`/products/${slug}`);
    await expect(this.section).toBeVisible({ timeout: 30_000 });
  }

  async scrollToReviews(): Promise<void> {
    await this.section.scrollIntoViewIfNeeded();
    await expect(this.form).toBeVisible();
  }

  async expectGuestFormLocked(): Promise<void> {
    await expect(this.formTitle).toHaveText("Увійдіть, щоб залишити відгук");
    await expect(this.commentInput).toBeDisabled();
    await expect(this.submitButton).toBeDisabled();
  }

  async setRating(stars: number): Promise<void> {
    await this.form.getByRole("radio", { name: `${stars} з 5` }).click();
  }

  async fillComment(text: string): Promise<void> {
    await this.commentInput.fill(text);
  }

  async submit(): Promise<void> {
    await this.submitButton.click();
  }

  reviewByText(comment: string): Locator {
    return this.page
      .getByTestId("product-review-item")
      .filter({ has: this.page.getByTestId("product-review-text").filter({ hasText: comment }) });
  }

  async expectReviewVisible(comment: string): Promise<Locator> {
    const item = this.reviewByText(comment);
    await expect(item).toBeVisible({ timeout: 15_000 });
    return item;
  }

  async expectReviewCount(comment: string, count: number): Promise<void> {
    await expect(this.reviewByText(comment)).toHaveCount(count);
  }
}

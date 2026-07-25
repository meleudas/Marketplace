import { expect, type Locator, type Page } from "@playwright/test";
import { realisticClick, realisticType } from "../helpers/realistic-pointer";

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

  async scrollToReviews(presentationPauseMs = 0): Promise<void> {
    if (presentationPauseMs > 0) {
      const writeLink = this.page.getByRole("button", { name: "Написати відгук" });
      if (await writeLink.isVisible().catch(() => false)) {
        await this.interact(writeLink, presentationPauseMs);
        await this.page.waitForTimeout(600);
      }
    }
    await this.section.scrollIntoViewIfNeeded();
    await expect(this.form).toBeVisible();
  }

  async expectGuestFormLocked(): Promise<void> {
    await expect(this.formTitle).toHaveText("Увійдіть, щоб залишити відгук");
    await expect(this.commentInput).toBeDisabled();
    await expect(this.submitButton).toBeDisabled();
  }

  /** Show that guest cannot write a review (hover locked controls). */
  async demonstrateGuestBlocked(presentationPauseMs = 500): Promise<void> {
    await this.expectGuestFormLocked();
    await this.interact(this.commentInput, presentationPauseMs);
    await this.page.waitForTimeout(400);
    await this.interact(this.submitButton, presentationPauseMs);
    await expect(this.commentInput).toBeDisabled();
    await expect(this.submitButton).toBeDisabled();
  }

  async setRating(stars: number, presentationPauseMs = 0): Promise<void> {
    const star = this.form.getByRole("radio", { name: `${stars} з 5` });
    await this.interact(star, presentationPauseMs);
  }

  async fillComment(text: string, presentationPauseMs = 0): Promise<void> {
    if (presentationPauseMs > 0) {
      await realisticType(this.page, this.commentInput, text, {
        hoverMs: Math.min(Math.max(presentationPauseMs, 350), 600),
        typeDelayMs: 55,
      });
      return;
    }
    await this.commentInput.fill(text);
  }

  async submit(presentationPauseMs = 0): Promise<void> {
    await this.interact(this.submitButton, presentationPauseMs);
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

  private async interact(target: Locator, presentationPauseMs = 0): Promise<void> {
    if (presentationPauseMs > 0) {
      await realisticClick(this.page, target, {
        hoverMs: Math.min(Math.max(presentationPauseMs, 350), 600),
      });
      return;
    }
    await target.click();
  }
}

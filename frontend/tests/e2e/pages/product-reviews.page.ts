import { expect, type Locator, type Page } from "@playwright/test";
import { realisticClick, realisticType } from "../helpers/realistic-pointer";

/** Matches sticky storefront header so the form title stays on screen. */
const RECORDING_SCROLL_TOP_OFFSET_PX = 88;

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
        await this.page.waitForTimeout(350);
      }
      await this.scrollFormTitleIntoRecordingView(400);
    } else {
      await this.section.scrollIntoViewIfNeeded();
    }
    await expect(this.form).toBeVisible();
    await expect(this.formTitle).toBeVisible();
  }

  /**
   * Keep «Увійдіть, щоб залишити відгук» fully visible under the sticky header.
   */
  async scrollFormTitleIntoRecordingView(settleMs = 800): Promise<void> {
    await this.formTitle.evaluate((el, offsetTop) => {
      const top = el.getBoundingClientRect().top + window.scrollY - offsetTop;
      window.scrollTo({ top: Math.max(0, top), behavior: "smooth" });
    }, RECORDING_SCROLL_TOP_OFFSET_PX);
    await this.page.waitForTimeout(settleMs);
    await expect(this.formTitle).toBeInViewport({ ratio: 0.9 });
  }

  async expectGuestFormLocked(): Promise<void> {
    await expect(this.formTitle).toHaveText("Увійдіть, щоб залишити відгук");
    await expect(this.commentInput).toBeDisabled();
    await expect(this.submitButton).toBeDisabled();
  }

  /** Outline the login prompt so it reads clearly on presentation video. */
  async highlightGuestLoginPrompt(holdMs = 2800): Promise<void> {
    await this.scrollFormTitleIntoRecordingView(400);
    await this.formTitle.evaluate((el) => {
      el.setAttribute("data-recording-highlight", "true");
      el.style.setProperty("outline", "3px solid #ee0290", "important");
      el.style.setProperty("outline-offset", "8px", "important");
      el.style.setProperty("border-radius", "10px", "important");
      el.style.setProperty(
        "box-shadow",
        "0 0 0 8px rgba(238, 2, 144, 0.28), 0 0 24px rgba(238, 2, 144, 0.45)",
        "important",
      );
      el.style.setProperty("background", "rgba(238, 2, 144, 0.12)", "important");
      el.style.setProperty("padding", "6px 10px", "important");
    });
    await this.page.waitForTimeout(holdMs);
  }

  async clearGuestLoginHighlight(): Promise<void> {
    await this.formTitle.evaluate((el) => {
      el.removeAttribute("data-recording-highlight");
      el.style.removeProperty("outline");
      el.style.removeProperty("outline-offset");
      el.style.removeProperty("border-radius");
      el.style.removeProperty("box-shadow");
      el.style.removeProperty("background");
      el.style.removeProperty("padding");
    });
  }

  /** Show that guest cannot write a review (highlight title, then hover locked controls). */
  async demonstrateGuestBlocked(
    presentationPauseMs = 500,
    options?: { highlightMs?: number },
  ): Promise<void> {
    await this.expectGuestFormLocked();
    await this.highlightGuestLoginPrompt(options?.highlightMs ?? 900);
    await this.interact(this.commentInput, presentationPauseMs);
    await this.page.waitForTimeout(200);
    await this.interact(this.submitButton, presentationPauseMs);
    await expect(this.commentInput).toBeDisabled();
    await expect(this.submitButton).toBeDisabled();
    await this.page.waitForTimeout(250);
    await this.clearGuestLoginHighlight();
  }

  async setRating(stars: number, presentationPauseMs = 0): Promise<void> {
    const star = this.form.getByRole("radio", { name: `${stars} з 5` });
    await this.interact(star, presentationPauseMs);
  }

  async fillComment(text: string, presentationPauseMs = 0): Promise<void> {
    if (presentationPauseMs > 0) {
      await realisticType(this.page, this.commentInput, text, {
        hoverMs: Math.min(Math.max(presentationPauseMs, 350), 600),
        typeDelayMs: 45,
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

  async expectPurchaseRequiredError(): Promise<void> {
    await expect(this.errorAlert).toBeVisible({ timeout: 15_000 });
    await expect(this.errorAlert).toHaveText(/Не можна залишити відгук до книги, яку ви не купували/i);
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

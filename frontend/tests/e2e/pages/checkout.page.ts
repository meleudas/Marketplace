import { expect, type Locator, type Page } from "@playwright/test";
import type { CheckoutAddressFixture } from "../fixtures/checkout.fixture";
import { checkoutTestAddress } from "../fixtures/checkout.fixture";

export class CheckoutPage {
  readonly page: Page;
  readonly heading: Locator;
  readonly submitButton: Locator;
  readonly errorAlert: Locator;
  readonly successTitle: Locator;

  constructor(page: Page) {
    this.page = page;
    this.heading = page.getByRole("heading", { name: "Оформлення замовлення", exact: true });
    this.submitButton = page.getByTestId("checkout-submit");
    this.errorAlert = page.getByTestId("checkout-error");
    this.successTitle = page.getByTestId("checkout-success");
  }

  async goto(): Promise<void> {
    await this.page.goto("/checkout");
  }

  async expectLoaded(): Promise<void> {
    await expect(this.heading).toBeVisible({ timeout: 30_000 });
    await expect(this.submitButton).toBeVisible();
  }

  async clearContactFields(): Promise<void> {
    await this.page.getByTestId("checkout-first-name").fill("");
    await this.page.getByTestId("checkout-last-name").fill("");
    const phoneInput = this.page.locator(".react-international-phone-input-container input").first();
    await phoneInput.fill("");
  }

  async fillContacts(
    address: Pick<CheckoutAddressFixture, "firstName" | "lastName" | "phone"> = checkoutTestAddress,
  ): Promise<void> {
    // Wait until auth profile hydration finishes, otherwise loadMe overwrites the form.
    await expect(this.page.locator('input[type="email"]')).not.toHaveValue("", {
      timeout: 15_000,
    });

    await this.page.getByTestId("checkout-first-name").fill(address.firstName);
    await this.page.getByTestId("checkout-last-name").fill(address.lastName);

    // react-international-phone with forceDialCode ignores Playwright fill() for the full E.164
    // value; type only the national digits after the dial code is present.
    const phoneInput = this.page.locator(".react-international-phone-input-container input").first();
    const nationalNumber = address.phone.replace(/^\+?380/, "").replace(/\D/g, "");
    await phoneInput.click();
    await phoneInput.press("ControlOrMeta+A");
    await phoneInput.press("Backspace");
    await expect
      .poll(async () => (await phoneInput.inputValue()).replace(/\D/g, ""), { timeout: 5_000 })
      .toMatch(/^380$/);
    await phoneInput.pressSequentially(nationalNumber, { delay: 20 });
    await expect
      .poll(async () => (await phoneInput.inputValue()).replace(/\D/g, ""), { timeout: 10_000 })
      .toBe(`380${nationalNumber}`);
  }

  async fillUkrPoshtaDelivery(
    address: Pick<CheckoutAddressFixture, "city" | "postalCode" | "branch"> = checkoutTestAddress,
  ): Promise<void> {
    await this.page.getByTestId("checkout-carrier-ukrposhta").click();
    await this.page.getByTestId("checkout-ukrposhta-city").fill(address.city);
    await this.page.getByTestId("checkout-ukrposhta-index").fill(address.postalCode);
    await this.page.getByTestId("checkout-ukrposhta-branch").fill(address.branch);
  }

  async selectCashPayment(): Promise<void> {
    await this.page.getByTestId("checkout-payment-cash").click();
  }

  async selectCardPayment(): Promise<void> {
    await this.page.getByTestId("checkout-payment-card").click();
  }

  async submit(): Promise<void> {
    await this.submitButton.click();
  }

  async expectValidationError(messagePart: string | RegExp): Promise<void> {
    await expect(this.errorAlert).toBeVisible();
    await expect(this.errorAlert).toContainText(messagePart);
  }

  async expectOrderCreated(): Promise<void> {
    await expect(this.successTitle).toBeVisible({ timeout: 30_000 });
    await expect(this.page.getByText(/Номер замовлення/i)).toBeVisible();
  }
}

export class CheckoutResultPage {
  readonly page: Page;
  readonly title: Locator;

  constructor(page: Page) {
    this.page = page;
    this.title = page.getByTestId("checkout-result-title");
  }

  async goto(orderNumber: string): Promise<void> {
    await this.page.goto(`/checkout/result?order_id=${encodeURIComponent(orderNumber)}`);
  }

  async expectPaid(): Promise<void> {
    await expect(this.title).toHaveText("Оплачено успішно!", { timeout: 30_000 });
  }

  async expectFailed(): Promise<void> {
    await expect(this.title).toHaveText("Оплата не вдалася", { timeout: 30_000 });
  }

  async expectPending(): Promise<void> {
    await expect(this.title).toHaveText("Очікує підтвердження", { timeout: 45_000 });
  }
}

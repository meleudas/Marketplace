import { expect, type Locator, type Page } from "@playwright/test";
import type { CheckoutAddressFixture } from "../fixtures/checkout.fixture";
import { checkoutTestAddress } from "../fixtures/checkout.fixture";
import { realisticClick, realisticType } from "../helpers/realistic-pointer";

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
    presentationPauseMs = 0,
  ): Promise<void> {
    // Wait until auth profile hydration finishes, otherwise loadMe overwrites the form.
    await expect(this.page.locator('input[type="email"]')).not.toHaveValue("", {
      timeout: 15_000,
    });

    const firstName = this.page.getByTestId("checkout-first-name");
    const lastName = this.page.getByTestId("checkout-last-name");
    if (presentationPauseMs > 0) {
      await realisticType(this.page, firstName, address.firstName, { hoverMs: 400, typeDelayMs: 70 });
      await this.page.waitForTimeout(350);
      await realisticType(this.page, lastName, address.lastName, { hoverMs: 400, typeDelayMs: 70 });
      await this.page.waitForTimeout(350);
    } else {
      await firstName.fill(address.firstName);
      await lastName.fill(address.lastName);
    }

    await this.fillPhone(address.phone, presentationPauseMs);
  }

  /** Wait for profile-hydrated first/last name; fill phone only when still empty. */
  async awaitPrefillContacts(
    phoneFallback: string = checkoutTestAddress.phone,
    presentationPauseMs = 0,
  ): Promise<void> {
    await expect(this.page.locator('input[type="email"]')).not.toHaveValue("", {
      timeout: 15_000,
    });
    await expect(this.page.getByTestId("checkout-first-name")).not.toHaveValue("", {
      timeout: 15_000,
    });
    await expect(this.page.getByTestId("checkout-last-name")).not.toHaveValue("", {
      timeout: 15_000,
    });

    const phoneInput = this.page.locator(".react-international-phone-input-container input").first();
    const digits = (await phoneInput.inputValue()).replace(/\D/g, "");
    if (digits.length <= 3) {
      await this.fillPhone(phoneFallback, presentationPauseMs);
    }
  }

  async fillMiddleName(middleName: string, presentationPauseMs = 0): Promise<void> {
    const input = this.page.getByPlaceholder("По батькові");
    await expect(input).toBeVisible();
    if (presentationPauseMs > 0) {
      await realisticType(this.page, input, middleName, { hoverMs: 400, typeDelayMs: 70 });
      return;
    }
    await input.fill(middleName);
  }

  async selectGiftWrap(presentationPauseMs = 0): Promise<void> {
    const giftLabel = this.page.getByText("Загорнути в подарункову упаковку", {
      exact: true,
    });
    await expect(giftLabel).toBeVisible();
    await this.interact(giftLabel, presentationPauseMs);
    await expect(this.page.getByText("Подарункова упаковка", { exact: true })).toBeVisible({
      timeout: 10_000,
    });
  }

  private async fillPhone(phone: string, presentationPauseMs = 0): Promise<void> {
    // react-international-phone with forceDialCode ignores Playwright fill() for the full E.164
    // value; type only the national digits after the dial code is present.
    const phoneInput = this.page.locator(".react-international-phone-input-container input").first();
    const nationalNumber = phone.replace(/^\+?380/, "").replace(/\D/g, "");
    if (presentationPauseMs > 0) {
      await realisticClick(this.page, phoneInput, { hoverMs: 400 });
    } else {
      await phoneInput.click();
    }
    await phoneInput.press("ControlOrMeta+A");
    await phoneInput.press("Backspace");
    await expect
      .poll(async () => (await phoneInput.inputValue()).replace(/\D/g, ""), { timeout: 5_000 })
      .toMatch(/^380$/);
    await phoneInput.pressSequentially(nationalNumber, {
      delay: presentationPauseMs > 0 ? 70 : 20,
    });
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

  /**
   * Nova Poshta via live api.novaposhta.ua suggestions: city → filter postomat → select.
   */
  async fillNovaPoshtaDelivery(
    options: {
      cityQuery?: string;
      branchFilter?: string;
      presentationPauseMs?: number;
    } = {},
  ): Promise<{ city: string; branch: string }> {
    const cityQuery = options.cityQuery ?? "Львів";
    const branchFilter = options.branchFilter ?? "Поштомат";
    const presentationPauseMs = options.presentationPauseMs ?? 0;

    const novaLabel = this.page.locator("label").filter({ hasText: /^Нова пошта$/ });
    await this.interact(novaLabel, presentationPauseMs);
    if (presentationPauseMs > 0) {
      await this.page.waitForTimeout(400);
    }

    const cityTrigger = this.page.getByRole("button", { name: "Місто", exact: true });
    await this.interact(cityTrigger, presentationPauseMs);

    const citySearch = this.page.getByPlaceholder("Почніть вводити назву міста...");
    await expect(citySearch).toBeVisible({ timeout: 10_000 });
    if (presentationPauseMs > 0) {
      await citySearch.pressSequentially(cityQuery, { delay: 80 });
    } else {
      await citySearch.fill(cityQuery);
    }

    const cityOption = this.page
      .locator("li")
      .filter({ hasText: new RegExp(cityQuery, "i") })
      .first();
    await expect(cityOption).toBeVisible({ timeout: 20_000 });
    if (presentationPauseMs > 0) {
      await this.page.waitForTimeout(600);
    }
    const city = (await cityOption.innerText()).trim();
    await this.interact(cityOption, presentationPauseMs);

    const branchTrigger = this.page.getByRole("button", {
      name: /Відділення або поштомат|Завантаження/,
    });
    await expect(branchTrigger).toBeEnabled({ timeout: 30_000 });
    await expect(branchTrigger).toHaveText(/Відділення або поштомат/, { timeout: 30_000 });
    if (presentationPauseMs > 0) {
      await this.page.waitForTimeout(500);
    }
    await this.interact(branchTrigger, presentationPauseMs);

    const branchSearch = this.page.getByPlaceholder("Пошук відділення...");
    await expect(branchSearch).toBeVisible({ timeout: 10_000 });
    if (presentationPauseMs > 0) {
      await branchSearch.pressSequentially(branchFilter, { delay: 70 });
    } else {
      await branchSearch.fill(branchFilter);
    }

    const postomat = this.page
      .locator("li")
      .filter({ hasText: /поштомат/i })
      .first();
    await expect(postomat).toBeVisible({ timeout: 20_000 });
    if (presentationPauseMs > 0) {
      await this.page.waitForTimeout(700);
    }
    const branch = (await postomat.innerText()).trim();
    await this.interact(postomat, presentationPauseMs);

    await expect(this.page.getByRole("button", { name: branch, exact: true })).toBeVisible({
      timeout: 10_000,
    });

    return { city, branch };
  }

  async selectCashPayment(presentationPauseMs = 0): Promise<void> {
    await this.interact(this.page.getByTestId("checkout-payment-cash"), presentationPauseMs);
  }

  async selectCardPayment(presentationPauseMs = 0): Promise<void> {
    await this.interact(this.page.getByTestId("checkout-payment-card"), presentationPauseMs);
  }

  async submit(presentationPauseMs = 0): Promise<void> {
    await this.interact(this.submitButton, presentationPauseMs);
  }

  async expectValidationError(messagePart: string | RegExp): Promise<void> {
    await expect(this.errorAlert).toBeVisible();
    await expect(this.errorAlert).toContainText(messagePart);
  }

  async expectOrderCreated(): Promise<void> {
    await expect(this.successTitle).toBeVisible({ timeout: 30_000 });
    await expect(this.page.getByText(/Номер замовлення/i)).toBeVisible();
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

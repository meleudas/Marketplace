import { expect, type Locator, type Page } from "@playwright/test";
import {
  formatExpectedCartPrice,
  getCatalogProductUnitPriceBySlug,
  type CartProductFixture,
} from "../fixtures/cart.fixture";
import { realisticClick } from "../helpers/realistic-pointer";

export class CartPage {
  readonly page: Page;
  readonly heading: Locator;
  readonly items: Locator;
  readonly headerCartLink: Locator;
  readonly headerCartBadge: Locator;

  constructor(page: Page) {
    this.page = page;
    this.heading = page.getByRole("heading", { name: "Кошик", exact: true });
    this.items = page.getByTestId("cart-item");
    this.headerCartLink = page.getByRole("link", { name: "Кошик" });
    this.headerCartBadge = page.getByTestId("header-cart-badge");
  }

  async goto(): Promise<void> {
    await this.page.goto("/cart");
    await expect(this.heading).toBeVisible();
  }

  cartItemByProductId(productId: number): Locator {
    return this.page.locator(`[data-testid="cart-item"][data-product-id="${productId}"]`);
  }

  async expectHeaderCount(count: number): Promise<void> {
    await expect(this.headerCartBadge).toHaveText(String(count), { timeout: 15_000 });
  }

  async expectProductInCart(
    product: Pick<CartProductFixture, "id" | "name" | "price">,
    quantity: number,
  ): Promise<void> {
    const item = this.cartItemByProductId(product.id);
    await expect(item).toBeVisible();
    await expect(item.getByTestId("cart-item-title")).toHaveText(product.name);
    await expect(item.getByTestId("cart-item-quantity")).toHaveText(String(quantity));
    await expect(item.getByTestId("cart-item-price")).toHaveText(
      formatExpectedCartPrice(product.price, quantity),
    );
  }

  async expectTotal(amountText: string): Promise<void> {
    await expect(this.page.getByTestId("cart-total")).toHaveText(amountText);
  }

  async readTotal(): Promise<string> {
    return (await this.page.getByTestId("cart-total").innerText()).trim();
  }

  async clickCheckoutCta(presentationPauseMs = 0): Promise<void> {
    const cta = this.page.getByTestId("cart-checkout-cta");
    await expect(cta).toBeVisible();
    await this.interact(cta, presentationPauseMs);
  }

  async openFromHeader(presentationPauseMs = 0): Promise<void> {
    await this.interact(this.headerCartLink, presentationPauseMs);
    await expect(this.heading).toBeVisible({ timeout: 30_000 });
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

export class CatalogCartActions {
  constructor(private readonly page: Page) {}

  private addedDialog(): Locator {
    return this.page.getByRole("dialog", { name: "Додано до кошика" });
  }

  async addFirstVisibleCatalogProduct(): Promise<CartProductFixture> {
    await this.page.goto("/catalog");
    await expect(this.page.getByRole("heading", { name: "Каталог", exact: true })).toBeVisible();
    await expect(this.page.getByTestId("product-card-add-to-cart").first()).toBeVisible({
      timeout: 30_000,
    });

    return this.addVisibleCatalogProductAt(0);
  }

  async addVisibleCatalogProductAt(index: number): Promise<CartProductFixture & { slug: string }> {
    const cards = this.page.locator("article").filter({
      has: this.page.getByTestId("product-card-add-to-cart"),
    });
    const card = cards.nth(index);
    await expect(card).toBeVisible({ timeout: 30_000 });

    const addButton = card.getByTestId("product-card-add-to-cart");
    const name = (await card.getByRole("heading", { level: 3 }).innerText()).trim();
    const productIdAttr = await addButton.getAttribute("data-product-id");
    expect(productIdAttr).toBeTruthy();

    const href = await card.locator('a[href^="/products/"]').first().getAttribute("href");
    const slug = (href ?? "").replace(/^\/products\//, "").split("?")[0];
    expect(slug).toBeTruthy();

    await addButton.click();
    const dialog = this.addedDialog();
    await expect(dialog).toBeVisible();
    await expect(dialog.getByText(name, { exact: true })).toBeVisible();

    // Dialog rounds display price; cart line totals use exact API unit price.
    const price = await getCatalogProductUnitPriceBySlug(this.page.request, slug);
    expect(price).toBeGreaterThan(0);

    return {
      id: Number(productIdAttr),
      name,
      slug,
      price,
    };
  }

  /**
   * Opens a random in-stock product card from the current catalog listing (PDP navigation).
   */
  async openRandomVisibleProduct(
    presentationPauseMs = 0,
  ): Promise<CartProductFixture & { slug: string }> {
    const cards = this.page.locator("article").filter({
      has: this.page.getByTestId("product-card-add-to-cart"),
    });
    await expect(cards.first()).toBeVisible({ timeout: 30_000 });
    const count = await cards.count();
    expect(count).toBeGreaterThan(0);
    const index = Math.floor(Math.random() * count);
    const card = cards.nth(index);

    const addButton = card.getByTestId("product-card-add-to-cart");
    const name = (await card.getByRole("heading", { level: 3 }).innerText()).trim();
    const productIdAttr = await addButton.getAttribute("data-product-id");
    expect(productIdAttr).toBeTruthy();

    const link = card.locator('a[href^="/products/"]').first();
    const href = await link.getAttribute("href");
    const slug = (href ?? "").replace(/^\/products\//, "").split("?")[0];
    expect(slug).toBeTruthy();

    await this.interact(link, presentationPauseMs);
    await expect(this.page).toHaveURL(new RegExp(`/products/${slug}`));
    await expect(this.page.getByTestId("product-details-add-to-cart")).toBeVisible({
      timeout: 30_000,
    });

    const price = await getCatalogProductUnitPriceBySlug(this.page.request, slug);
    expect(price).toBeGreaterThan(0);

    return {
      id: Number(productIdAttr),
      name,
      slug,
      price,
    };
  }

  async addProductFromCatalogCard(product: CartProductFixture): Promise<void> {
    await this.page.goto("/catalog");
    await expect(this.page.getByRole("heading", { name: "Каталог", exact: true })).toBeVisible();

    const addButton = this.page.locator(
      `[data-testid="product-card-add-to-cart"][data-product-id="${product.id}"]`,
    );
    await expect(addButton).toBeVisible({ timeout: 30_000 });
    await addButton.click();
    await expect(this.addedDialog()).toBeVisible();
  }

  async dismissAddedDialogContinueShopping(presentationPauseMs = 0): Promise<void> {
    const button = this.page.getByRole("button", { name: "Продовжити покупки" });
    await this.interact(button, presentationPauseMs);
    await expect(this.addedDialog()).toHaveCount(0);
  }

  async goToCartFromDialog(presentationPauseMs = 0): Promise<void> {
    const button = this.page.getByRole("button", { name: "Перейти до кошика" });
    await this.interact(button, presentationPauseMs);
    await expect(this.page.getByRole("heading", { name: "Кошик", exact: true })).toBeVisible();
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

export class ProductDetailsCartActions {
  constructor(private readonly page: Page) {}

  private addedDialog(): Locator {
    return this.page.getByRole("dialog", { name: "Додано до кошика" });
  }

  async gotoProduct(slug: string): Promise<void> {
    await this.page.goto(`/products/${slug}`);
    await expect(this.page.getByTestId("product-details-add-to-cart")).toBeVisible({
      timeout: 30_000,
    });
    // Auth bootstrap on PDP can finish after the add button is painted.
    await expect(this.page.getByRole("link", { name: /Профіль/ })).toBeVisible({
      timeout: 15_000,
    });
  }

  async setQuantity(quantity: number, presentationPauseMs = 0): Promise<void> {
    expect(quantity).toBeGreaterThanOrEqual(1);
    const increase = this.page
      .getByTestId("product-details-add-to-cart")
      .locator("xpath=..")
      .getByRole("button", { name: "Збільшити кількість" });
    const stepperValue = increase.locator("xpath=preceding-sibling::*[1]");

    for (let current = 1; current < quantity; current++) {
      await this.interact(increase, presentationPauseMs);
      await expect(stepperValue).toHaveText(String(current + 1), { timeout: 5_000 });
      if (presentationPauseMs > 0) {
        await this.page.waitForTimeout(350);
      }
    }

    await expect(stepperValue).toHaveText(String(quantity));
  }

  async addToCart(presentationPauseMs = 0): Promise<void> {
    const button = this.page.getByTestId("product-details-add-to-cart");
    await this.interact(button, presentationPauseMs);
    await expect(this.addedDialog()).toBeVisible({ timeout: 15_000 });
  }

  async dismissAddedDialog(presentationPauseMs = 0): Promise<void> {
    const button = this.page.getByRole("button", { name: "Продовжити покупки" });
    await this.interact(button, presentationPauseMs);
    await expect(this.addedDialog()).toHaveCount(0);
  }

  async goToCartFromDialog(presentationPauseMs = 0): Promise<void> {
    const button = this.page.getByRole("button", { name: "Перейти до кошика" });
    await this.interact(button, presentationPauseMs);
    await expect(this.page.getByRole("heading", { name: "Кошик", exact: true })).toBeVisible();
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

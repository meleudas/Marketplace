import { expect, type Locator, type Page, type Response } from "@playwright/test";
import type { CatalogSearchFixture } from "../fixtures/catalog.fixture";
import { realisticClick, realisticType } from "../helpers/realistic-pointer";

type SearchRequestPredicate = (url: URL) => boolean;

export class CatalogPage {
  readonly page: Page;
  readonly heading: Locator;
  readonly sidebar: Locator;
  readonly selectedFilters: Locator;
  readonly productLinks: Locator;
  readonly emptyState: Locator;
  readonly pagination: Locator;
  readonly pageSizeSelect: Locator;

  constructor(page: Page) {
    this.page = page;
    this.heading = page.getByRole("heading", { name: "Каталог", exact: true });
    this.sidebar = page.getByRole("complementary", { name: "Фільтри каталогу" });
    this.selectedFilters = page.getByRole("region", { name: "Обрані фільтри" });
    this.productLinks = page.locator('a[href^="/products/"]');
    this.emptyState = page.getByRole("status").filter({
      hasText: "Товарів за обраними фільтрами не знайдено",
    });
    this.pagination = page.getByRole("navigation", { name: "Пагінація" });
    this.pageSizeSelect = page.getByLabel("Показати по:");
  }

  async goto(path = "/catalog"): Promise<void> {
    await this.page.goto(path);
    await this.waitUntilLoaded();
  }

  /** Land on the filtered catalog state that ends the 01-catalog presentation clip. */
  async gotoPresentationEndState(subcategorySlug: string): Promise<void> {
    const params = new URLSearchParams({
      format: "паперовий",
      minPrice: "100",
      sort: "price_asc",
    });
    await this.goto(`/catalog/${subcategorySlug}?${params.toString()}`);
    await this.expectBrowserSearchParams({
      format: "паперовий",
      minPrice: "100",
      sort: "price_asc",
    });
  }

  async waitUntilLoaded(): Promise<void> {
    await expect(this.heading).toBeVisible();
    await expect(this.page.getByText(/^Знайдено \d+ товар/)).toBeVisible();
    await expect(this.sidebar).toBeVisible();
    // When idle, aria-busy is omitted (not aria-busy="false").
    await expect(this.sidebar).not.toHaveAttribute("aria-busy", "true");
  }

  async openCategoryFromHeader(
    rootName: string,
    subcategoryName: string,
    subcategoryId: number,
    presentationPauseMs = 0,
  ): Promise<void> {
    const catalogButton = this.page.getByRole("button", { name: "Каталог", exact: true });
    await this.interact(catalogButton, presentationPauseMs);
    const menu = this.page.getByRole("dialog");
    await expect(menu.getByRole("heading", { name: "Каталог", exact: true })).toBeVisible();
    if (presentationPauseMs > 0) {
      await this.page.waitForTimeout(presentationPauseMs);
    }

    const rootButton = menu.getByRole("button", { name: rootName, exact: true });
    await this.interact(rootButton, presentationPauseMs);
    await expect(menu.getByRole("heading", { name: rootName, exact: true })).toBeVisible();
    if (presentationPauseMs > 0) {
      await this.page.waitForTimeout(presentationPauseMs);
    }

    const subcategoryButton = menu.getByRole("button", { name: subcategoryName, exact: true });
    await this.waitForSearchRequest(
      () => this.interact(subcategoryButton, presentationPauseMs),
      (url) =>
        (url.searchParams.get("pageSize") === "20" || url.searchParams.get("pageSize") === "21") &&
        url.searchParams.getAll("categoryIds").includes(String(subcategoryId)),
    );
  }

  async applyCategory(
    categoryName: string,
    expectedCategoryIds: readonly number[],
  ): Promise<CatalogSearchFixture> {
    const checkbox = this.sidebar.getByRole("checkbox", { name: categoryName, exact: true });
    return this.waitForSearch(
      () => this.checkSidebarCheckbox(checkbox),
      (url) => {
        const ids = url.searchParams.getAll("categoryIds");
        return expectedCategoryIds.every((id) => ids.includes(String(id)));
      },
    );
  }

  async applyFormat(
    label: "Електронна" | "Паперова",
    value: "електронний" | "паперовий",
    presentationPauseMs = 0,
  ): Promise<CatalogSearchFixture> {
    const checkbox = this.sidebar.getByRole("checkbox", { name: label, exact: true });
    return this.waitForSearch(
      () => this.checkSidebarCheckbox(checkbox, presentationPauseMs),
      (url) => url.searchParams.get("format") === value,
    );
  }

  async applyAuthor(
    authorLabel: string,
    authorValue: string,
  ): Promise<CatalogSearchFixture> {
    const checkbox = this.sidebar.getByRole("checkbox", {
      name: new RegExp(`^${escapeRegex(authorLabel)} \\(\\d+\\)$`),
    });

    return this.waitForSearch(
      () => this.checkSidebarCheckbox(checkbox),
      (url) => url.searchParams.getAll("authors").includes(authorValue),
    );
  }

  async applyMinimumPrice(minPrice: number, presentationPauseMs = 0): Promise<CatalogSearchFixture> {
    const input = this.sidebar.getByLabel("Від", { exact: true });
    return this.waitForSearch(
      async () => {
        await expect(this.sidebar).not.toHaveAttribute("aria-busy", "true");
        if (presentationPauseMs > 0) {
          await realisticType(this.page, input, String(minPrice), {
            hoverMs: Math.min(presentationPauseMs, 500),
          });
        } else {
          await input.fill(String(minPrice));
        }
        await input.blur();
      },
      (url) => url.searchParams.get("minPrice") === String(minPrice),
    );
  }

  async resetFilters(): Promise<CatalogSearchFixture> {
    return this.waitForSearch(
      async () => {
        await expect(this.sidebar).not.toHaveAttribute("aria-busy", "true");
        await this.sidebar.getByRole("button", { name: "Очистити" }).click();
      },
      (url) =>
        !url.searchParams.has("categoryIds") &&
        !url.searchParams.has("authors") &&
        !url.searchParams.has("format") &&
        !url.searchParams.has("minPrice") &&
        !url.searchParams.has("maxPrice"),
    );
  }

  async selectPageSize(size: 10 | 20 | 30 | 40 | 50): Promise<CatalogSearchFixture> {
    return this.waitForSearch(
      async () => {
        await expect(this.pageSizeSelect).toBeVisible();
        await this.pageSizeSelect.selectOption(String(size));
      },
      (url) => url.searchParams.get("pageSize") === String(size),
    );
  }

  async selectSort(
    label: "За популярністю" | "За новизною" | "Від найдешевших" | "Від найдорожчих",
    value: "relevance" | "newest" | "price_asc" | "price_desc",
    presentationPauseMs = 0,
  ): Promise<CatalogSearchFixture> {
    return this.waitForSearch(
      async () => {
        const trigger = this.page.locator("button[aria-haspopup='listbox']").filter({
          hasText: /За популярністю|За новизною|Від найдешевших|Від найдорожчих/,
        });
        await expect(trigger).toBeVisible();
        await this.interact(trigger, presentationPauseMs);
        const listbox = this.page.getByRole("listbox");
        await expect(listbox).toBeVisible();
        if (presentationPauseMs > 0) {
          await this.page.waitForTimeout(presentationPauseMs);
        }
        const option = listbox.getByRole("button", { name: label, exact: true });
        await this.interact(option, presentationPauseMs);
      },
      (url) => url.searchParams.get("sort") === value,
    );
  }

  async goToPage(pageNumber: number): Promise<CatalogSearchFixture> {
    return this.waitForSearch(
      async () => {
        await expect(this.pagination).toBeVisible();
        await this.pagination.getByRole("button", { name: String(pageNumber), exact: true }).click();
      },
      (url) => url.searchParams.get("page") === String(pageNumber),
    );
  }

  async expectBrowserSearchParam(name: string, value: string | null): Promise<void> {
    await expect
      .poll(() => new URL(this.page.url()).searchParams.get(name), { timeout: 15_000 })
      .toBe(value);
  }

  async expectBrowserSearchParams(params: Record<string, string>): Promise<void> {
    await expect
      .poll(() => {
        const search = new URL(this.page.url()).searchParams;
        return Object.fromEntries(Object.keys(params).map((key) => [key, search.get(key)]));
      }, { timeout: 15_000 })
      .toEqual(params);
  }

  async expectSelectedFilter(label: string): Promise<void> {
    await expect(this.selectedFilters.getByText(label, { exact: true })).toBeVisible();
  }

  async expectRenderedProducts(result: CatalogSearchFixture): Promise<void> {
    await expect(this.productLinks).toHaveCount(result.items.length);
    const renderedSlugs = await this.productLinks.evaluateAll((links) =>
      links.map((link) => new URL((link as HTMLAnchorElement).href).pathname.split("/").pop()),
    );
    expect(renderedSlugs).toEqual(result.items.map((item) => item.slug));
  }

  async waitForSearch(
    action: () => Promise<unknown>,
    predicate: SearchRequestPredicate,
  ): Promise<CatalogSearchFixture> {
    const response = await this.captureSearchResponse(action, predicate);
    await expectSearchSucceeded(response);
    const result = (await response.json()) as CatalogSearchFixture;
    await expect(this.page.getByText(new RegExp(`^Знайдено ${result.total} товар`))).toBeVisible();
    await expect(this.sidebar).toBeVisible();
    await expect(this.sidebar).not.toHaveAttribute("aria-busy", "true");
    return result;
  }

  private async checkSidebarCheckbox(
    checkbox: Locator,
    presentationPauseMs = 0,
  ): Promise<void> {
    await expect(async () => {
      await expect(this.sidebar).not.toHaveAttribute("aria-busy", "true");
      if (!(await checkbox.isChecked())) {
        // Native input is visually hidden; the label receives the click / hover.
        const label = checkbox.locator("xpath=..");
        await this.interact(label, presentationPauseMs);
      }
      await expect(checkbox).toBeChecked();
    }).toPass({ timeout: 15_000 });
  }

  /** Instant click in e2e; hover → press → release when recording presentations. */
  private async interact(target: Locator, presentationPauseMs = 0): Promise<void> {
    if (presentationPauseMs > 0) {
      await realisticClick(this.page, target, {
        hoverMs: Math.min(Math.max(presentationPauseMs, 350), 600),
      });
      return;
    }
    await target.click();
  }

  private async waitForSearchRequest(
    action: () => Promise<unknown>,
    predicate: SearchRequestPredicate,
  ): Promise<void> {
    const response = await this.captureSearchResponse(action, predicate);
    await expectSearchSucceeded(response);
    await this.waitUntilLoaded();
  }

  private async captureSearchResponse(
    action: () => Promise<unknown>,
    predicate: SearchRequestPredicate,
  ): Promise<Response> {
    const responsePromise = this.page.waitForResponse((response) => {
      const url = new URL(response.url());
      return (
        response.request().method() === "GET" &&
        url.pathname.endsWith("/catalog/products/search") &&
        predicate(url)
      );
    });

    await action();
    return responsePromise;
  }
}

async function expectSearchSucceeded(response: Response): Promise<void> {
  expect(response.ok(), `Catalog search failed: ${response.status()} ${response.url()}`).toBe(true);
}

function escapeRegex(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

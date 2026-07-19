import { expect, test } from "@playwright/test";
import {
  getCatalogTestData,
  getCategoryAndDescendantIds,
  type CatalogTestData,
} from "../fixtures/catalog.fixture";
import { CatalogPage } from "../pages/catalog.page";

test.describe("Catalog and filters", () => {
  let data: CatalogTestData;

  test.beforeAll(async ({ request }) => {
    data = await getCatalogTestData(request);
  });

  test("opens a header subcategory and displays it as selected", async ({ page }) => {
    const catalog = new CatalogPage(page);
    await page.goto("/");

    await catalog.openCategoryFromHeader(
      data.rootCategory.name,
      data.subcategory.name,
      data.subcategory.id,
    );

    await expect(page).toHaveURL(new RegExp(`/catalog/${data.subcategory.slug}(?:\\?|$)`));
    await catalog.expectSelectedFilter(data.subcategory.name);
  });

  for (const format of [
    { label: "Паперова", value: "паперовий" },
    { label: "Електронна", value: "електронний" },
  ] as const) {
    test(`applies the ${format.label.toLowerCase()} format filter`, async ({ page }) => {
      const catalog = new CatalogPage(page);
      await catalog.goto();

      const result = await catalog.applyFormat(format.label, format.value);

      expect(result.total).toBeGreaterThan(0);
      await catalog.expectBrowserSearchParam("format", format.value);
      await catalog.expectSelectedFilter(format.label);
      await catalog.expectRenderedProducts(result);
    });
  }

  test("applies an author filter", async ({ page }) => {
    const catalog = new CatalogPage(page);
    await catalog.goto();

    const result = await catalog.applyAuthor(data.author.label, data.author.value);

    expect(result.total).toBeGreaterThan(0);
    await catalog.expectBrowserSearchParam("authors", data.author.value);
    await catalog.expectSelectedFilter(data.author.label);
    await catalog.expectRenderedProducts(result);
  });

  test("applies category and format filters together", async ({ page }) => {
    const catalog = new CatalogPage(page);
    const categoryIds = getCategoryAndDescendantIds(data.categories, data.rootCategory.id);
    await catalog.goto();

    await catalog.applyCategory(data.rootCategory.name, categoryIds);
    const result = await catalog.applyFormat("Паперова", "паперовий");

    await catalog.expectBrowserSearchParam("categories", data.rootCategory.slug);
    await catalog.expectBrowserSearchParam("format", "паперовий");
    await catalog.expectSelectedFilter(data.rootCategory.name);
    await catalog.expectSelectedFilter("Паперова");
    expect(result.items.every((product) => categoryIds.includes(product.categoryId))).toBe(true);
  });

  test("resets all applied filters", async ({ page }) => {
    const catalog = new CatalogPage(page);
    const categoryIds = getCategoryAndDescendantIds(data.categories, data.rootCategory.id);

    await catalog.waitForSearch(
      () =>
        page.goto(
          `/catalog?categories=${encodeURIComponent(data.rootCategory.slug)}&format=${encodeURIComponent("паперовий")}`,
        ),
      (url) => {
        const pageSize = url.searchParams.get("pageSize");
        return (
          (pageSize === "20" || pageSize === "21") &&
          url.searchParams.get("format") === "паперовий" &&
          categoryIds.every((id) => url.searchParams.getAll("categoryIds").includes(String(id)))
        );
      },
    );
    await catalog.expectSelectedFilter(data.rootCategory.name);
    await catalog.expectSelectedFilter("Паперова");

    const result = await catalog.resetFilters();

    const url = new URL(page.url());
    expect(url.searchParams.has("categories")).toBe(false);
    expect(url.searchParams.has("format")).toBe(false);
    await expect(catalog.selectedFilters).toHaveCount(0);
    expect(result.total).toBeGreaterThan(0);
  });

  test("renders only products matching category and price filters", async ({ page }) => {
    const catalog = new CatalogPage(page);
    const categoryIds = getCategoryAndDescendantIds(data.categories, data.rootCategory.id);
    const minPrice = 100;
    const maxPrice = 300;

    const result = await catalog.waitForSearch(
      () =>
        page.goto(
          `/catalog?categories=${encodeURIComponent(data.rootCategory.slug)}&minPrice=${minPrice}&maxPrice=${maxPrice}`,
        ),
      (url) => {
        const pageSize = url.searchParams.get("pageSize");
        return (
          (pageSize === "20" || pageSize === "21") &&
          url.searchParams.get("minPrice") === String(minPrice) &&
          url.searchParams.get("maxPrice") === String(maxPrice) &&
          categoryIds.every((id) => url.searchParams.getAll("categoryIds").includes(String(id)))
        );
      },
    );

    expect(result.items.length).toBeGreaterThan(0);
    expect(
      result.items.every(
        (product) =>
          categoryIds.includes(product.categoryId) &&
          product.price >= minPrice &&
          product.price <= maxPrice,
      ),
    ).toBe(true);
    await catalog.expectRenderedProducts(result);
  });

  test("shows an empty state when filters match no products", async ({ page }) => {
    const catalog = new CatalogPage(page);
    await catalog.goto();

    const result = await catalog.applyMinimumPrice(999_999);

    expect(result.total).toBe(0);
    await expect(catalog.emptyState).toBeVisible();
    await expect(catalog.productLinks).toHaveCount(0);
  });
});

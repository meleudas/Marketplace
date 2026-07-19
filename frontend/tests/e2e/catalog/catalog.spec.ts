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

  test("keeps header category selected after applying a sidebar format filter", async ({
    page,
  }) => {
    const catalog = new CatalogPage(page);
    await page.goto("/");

    await catalog.openCategoryFromHeader(
      data.rootCategory.name,
      data.subcategory.name,
      data.subcategory.id,
    );
    await catalog.expectSelectedFilter(data.subcategory.name);

    await catalog.applyFormat("Паперова", "паперовий");

    await expect(page).toHaveURL(new RegExp(`/catalog/${data.subcategory.slug}`));
    await catalog.expectSelectedFilter(data.subcategory.name);
    await catalog.expectSelectedFilter("Паперова");
    await expect(
      catalog.sidebar.getByRole("checkbox", { name: data.subcategory.name, exact: true }),
    ).toBeChecked();
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
      await catalog.expectSelectedFilter(format.label);
      await catalog.expectRenderedProducts(result);
    });
  }

  test("applies an author filter", async ({ page }) => {
    const catalog = new CatalogPage(page);
    await catalog.goto();

    const result = await catalog.applyAuthor(data.author.label, data.author.value);

    expect(result.total).toBeGreaterThan(0);
    await catalog.expectSelectedFilter(data.author.label);
    await catalog.expectRenderedProducts(result);
  });

  test("applies category and format filters together", async ({ page }) => {
    const catalog = new CatalogPage(page);
    const categoryIds = getCategoryAndDescendantIds(data.categories, data.rootCategory.id);
    await catalog.goto();

    await catalog.applyCategory(data.rootCategory.name, categoryIds);
    const result = await catalog.applyFormat("Паперова", "паперовий");

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

  test("syncs applied filters to the browser URL", async ({ page }) => {
    const catalog = new CatalogPage(page);
    await catalog.goto();

    await catalog.applyFormat("Паперова", "паперовий");
    await catalog.expectBrowserSearchParam("format", "паперовий");

    await catalog.applyAuthor(data.author.label, data.author.value);
    await catalog.expectBrowserSearchParams({
      format: "паперовий",
      authors: data.author.value,
    });
  });

  test("changes page size and reflects it in API, UI, and URL", async ({ page }) => {
    const catalog = new CatalogPage(page);
    await catalog.goto();

    const result = await catalog.selectPageSize(10);

    expect(result.pageSize).toBe(10);
    expect(result.items.length).toBeLessThanOrEqual(10);
    await expect(catalog.productLinks).toHaveCount(result.items.length);
    await expect(catalog.pageSizeSelect).toHaveValue("10");
    await catalog.expectBrowserSearchParam("perPage", "10");
  });

  test("paginates to the next page and updates the URL", async ({ page }) => {
    const catalog = new CatalogPage(page);
    await catalog.goto();

    const firstPage = await catalog.selectPageSize(10);
    expect(firstPage.total).toBeGreaterThan(10);
    const firstPageSlugs = firstPage.items.map((item) => item.slug);

    const secondPage = await catalog.goToPage(2);

    expect(secondPage.page).toBe(2);
    expect(secondPage.items.map((item) => item.slug)).not.toEqual(firstPageSlugs);
    await catalog.expectRenderedProducts(secondPage);
    await catalog.expectBrowserSearchParam("page", "2");
    await expect(catalog.pagination.getByRole("button", { name: "2", exact: true })).toHaveAttribute(
      "aria-current",
      "page",
    );
  });

  test("applies sorting by price ascending and syncs the URL", async ({ page }) => {
    const catalog = new CatalogPage(page);
    await catalog.goto();

    const result = await catalog.selectSort("Від найдешевших", "price_asc");

    expect(result.items.length).toBeGreaterThan(1);
    const prices = result.items.map((item) => item.price);
    expect(prices).toEqual([...prices].sort((left, right) => left - right));
    await catalog.expectRenderedProducts(result);
    await catalog.expectBrowserSearchParam("sort", "price_asc");
    await expect(
      page.locator("button[aria-haspopup='listbox']").filter({ hasText: "Від найдешевших" }),
    ).toBeVisible();
  });
});

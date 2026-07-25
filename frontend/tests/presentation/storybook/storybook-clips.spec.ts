import path from "node:path";
import { ASSETS, expect, hold, test } from "../helpers/record-clip";

const STORY = (id: string) => `/iframe.html?id=${id}&viewMode=story`;

async function openStory(page: import("@playwright/test").Page, id: string) {
  await page.goto(STORY(id), { waitUntil: "load" });
  await page.waitForFunction(
    () => {
      const root = document.getElementById("storybook-root");
      return Boolean(root && !root.hasAttribute("hidden") && root.childElementCount > 0);
    },
    null,
    { timeout: 30_000 },
  );
  await hold(page, 600);
}

async function setProductCardStep(
  page: import("@playwright/test").Page,
  step: "default" | "outOfStock" | "discount" | "hoverCard" | "hoverCart",
) {
  await page.evaluate((next) => {
    window.__productCardTourSetStep?.(next);
  }, step);
  await expect(page.getByTestId("product-card-tour")).toHaveAttribute("data-tour-step", step);
}

async function setTextFieldStep(
  page: import("@playwright/test").Page,
  step: "default" | "hover" | "focused" | "error",
) {
  await page.evaluate((next) => {
    window.__textFieldTourSetStep?.(next);
  }, step);
  await expect(page.getByTestId("text-field-tour")).toHaveAttribute("data-tour-step", step);
}

test.describe("Storybook presentation clips", () => {
  test("01-product-card-states", async ({ recordClip }) => {
    await recordClip(path.join(ASSETS.storybook, "01-product-card-states.webm"), async (page) => {
      // Single story load — no iframe remount flash between states.
      await openStory(page, "shared-productcard--presentation-tour");
      await expect(page.getByTestId("product-card-tour")).toBeVisible();
      await expect(page.getByRole("heading", { name: "Книга Відлуння старих книжок" })).toBeVisible();

      await setProductCardStep(page, "default");
      await hold(page, 2400);

      await setProductCardStep(page, "outOfStock");
      await hold(page, 2400);

      await setProductCardStep(page, "discount");
      await hold(page, 2400);

      await setProductCardStep(page, "hoverCard");
      const cardNav = page.locator("#storybook-root a").first();
      await expect(cardNav).toBeVisible();
      await cardNav.hover();
      await hold(page, 2800);

      await setProductCardStep(page, "hoverCart");
      const addButton = page.getByTestId("product-card-add-to-cart");
      await expect(addButton).toBeVisible();
      await addButton.hover();
      await hold(page, 3000);
    });
  });

  test("02-textfield-states", async ({ recordClip }) => {
    await recordClip(path.join(ASSETS.storybook, "02-textfield-states.webm"), async (page) => {
      await openStory(page, "shared-textfield--presentation-tour");
      await expect(page.getByTestId("text-field-tour")).toBeVisible();

      await setTextFieldStep(page, "default");
      await hold(page, 2200);

      await setTextFieldStep(page, "hover");
      await expect(page.getByTestId("text-field-control")).toBeVisible();
      await hold(page, 2800);

      await setTextFieldStep(page, "focused");
      const focusInput = page.getByLabel("Ім'я");
      await expect(focusInput).toBeVisible();
      await focusInput.click();
      await hold(page, 2800);

      await setTextFieldStep(page, "error");
      await hold(page, 3200);
    });
  });
});

import { expect, type Page, type Response } from "@playwright/test";
import { realisticClick, realisticType } from "../../e2e/helpers/realistic-pointer";
import { hold } from "./record-clip";

function isLoginPostResponse(response: Response): boolean {
  if (response.request().method() !== "POST") {
    return false;
  }

  try {
    return new URL(response.url()).pathname.endsWith("/auth/login");
  } catch {
    return /\/auth\/login(?:\?|$)/.test(response.url());
  }
}

/**
 * Login on the already-open `/auth/login?redirect=…` screen (presentation style).
 * Does not navigate away first — preserves the redirect query.
 */
export async function presentationLoginOnCurrentPage(
  page: Page,
  credentials: { email: string; password: string },
  options?: { expectPath?: RegExp },
): Promise<void> {
  await expect(page.getByRole("heading", { name: "Вхід" })).toBeVisible({ timeout: 30_000 });

  const emailInput = page.locator("#login-email");
  const passwordInput = page.locator("#login-password");
  const submitButton = page.getByRole("button", { name: "Увійти", exact: true });

  await realisticType(page, emailInput, credentials.email, { hoverMs: 400, typeDelayMs: 70 });
  await hold(page, 400);
  await realisticType(page, passwordInput, credentials.password, { hoverMs: 400, typeDelayMs: 70 });
  await hold(page, 500);

  const loginResponsePromise = page.waitForResponse(isLoginPostResponse, { timeout: 30_000 });
  await realisticClick(page, submitButton, { hoverMs: 450 });
  const loginResponse = await loginResponsePromise;
  expect(
    loginResponse.ok(),
    `Presentation login failed: HTTP ${loginResponse.status()}`,
  ).toBeTruthy();

  await expect
    .poll(async () => page.evaluate(() => window.localStorage.getItem("accessToken")), {
      timeout: 30_000,
    })
    .not.toBeNull();

  if (options?.expectPath) {
    await page.waitForURL(options.expectPath, { timeout: 30_000 });
  } else {
    await page.waitForURL((url) => !url.pathname.startsWith("/auth/login"), {
      timeout: 30_000,
    });
  }
}

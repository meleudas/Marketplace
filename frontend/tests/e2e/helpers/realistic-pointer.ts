import type { Locator, Page } from "@playwright/test";

type RealisticClickOptions = {
  /** Leave the pointer hovering so :hover styles read on video. */
  hoverMs?: number;
  /** Mouse-move interpolation steps toward the target. */
  moveSteps?: number;
  /** Brief pause while the primary button is held (pressed state). */
  pressMs?: number;
};

/**
 * Move → hover → press → release. Used by presentation recordings.
 * Regular e2e tests should keep instant locator.click().
 */
export async function realisticClick(
  page: Page,
  target: Locator,
  options: RealisticClickOptions = {},
): Promise<void> {
  const hoverMs = options.hoverMs ?? 450;
  const moveSteps = options.moveSteps ?? 18;
  const pressMs = options.pressMs ?? 100;

  await target.scrollIntoViewIfNeeded();
  const box = await target.boundingBox();
  if (!box) {
    await target.hover();
    await page.waitForTimeout(hoverMs);
    await target.click();
    return;
  }

  await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2, {
    steps: moveSteps,
  });
  await page.waitForTimeout(hoverMs);
  await page.mouse.down();
  await page.waitForTimeout(pressMs);
  await page.mouse.up();
}

export async function realisticType(
  page: Page,
  target: Locator,
  text: string,
  options: RealisticClickOptions & { typeDelayMs?: number } = {},
): Promise<void> {
  await realisticClick(page, target, options);
  await page.keyboard.press("ControlOrMeta+A");
  await target.pressSequentially(text, { delay: options.typeDelayMs ?? 90 });
}

import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { test as base, type Page } from "@playwright/test";
import { convertWebmToMp4, probeDurationSeconds } from "./convert-to-mp4";

const REPO_ROOT = path.resolve(__dirname, "../../../..");

export const ASSETS = {
  storybook: path.join(REPO_ROOT, "presentation-assets", "storybook"),
  flows: path.join(REPO_ROOT, "presentation-assets", "flows"),
} as const;

export async function hold(page: Page, ms: number): Promise<void> {
  await page.waitForTimeout(ms);
}

type RecordClipFixtures = {
  recordClip: (
    absolutePath: string,
    run: (page: Page) => Promise<void>,
    options?: { width?: number; height?: number },
  ) => Promise<{ durationSec: number }>;
};

export const test = base.extend<RecordClipFixtures>({
  // Playwright fixture setter (not React's `use` hook).
  recordClip: async ({ browser, baseURL }, provide) => {
    await provide(async (absolutePath, run, options) => {
      const width = options?.width ?? 1280;
      const height = options?.height ?? 720;
      const tmp = fs.mkdtempSync(path.join(os.tmpdir(), "booktop-pres-"));
      const wantsMp4 = absolutePath.toLowerCase().endsWith(".mp4");

      const context = await browser.newContext({
        baseURL,
        viewport: { width, height },
        recordVideo: { dir: tmp, size: { width, height } },
        locale: "uk-UA",
      });

      const page = await context.newPage();

      try {
        await run(page);
        await hold(page, 400);
      } finally {
        await context.close();
      }

      const webm = fs.readdirSync(tmp).find((file) => file.endsWith(".webm"));
      if (!webm) {
        fs.rmSync(tmp, { recursive: true, force: true });
        throw new Error(`No video produced for ${absolutePath}`);
      }

      const webmPath = path.join(tmp, webm);
      fs.mkdirSync(path.dirname(absolutePath), { recursive: true });
      if (fs.existsSync(absolutePath)) {
        fs.unlinkSync(absolutePath);
      }

      if (wantsMp4) {
        convertWebmToMp4(webmPath, absolutePath);
      } else {
        fs.copyFileSync(webmPath, absolutePath);
      }

      const durationSec = probeDurationSeconds(absolutePath);
      fs.rmSync(tmp, { recursive: true, force: true });
      return { durationSec };
    });
  },
});

export { expect } from "@playwright/test";

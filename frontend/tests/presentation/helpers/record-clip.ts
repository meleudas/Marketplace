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

const BROWSER_CHROME_HEIGHT = 52;

async function installAddressBar(page: Page): Promise<void> {
  await page.addInitScript(({ height }) => {
    const install = () => {
      if (document.getElementById("recording-browser-chrome")) {
        return;
      }

      const style = document.createElement("style");
      style.textContent = `
        html { background: #eef1f5 !important; }
        body { padding-top: ${height}px !important; }
        #recording-browser-chrome {
          position: fixed !important;
          inset: 0 0 auto 0 !important;
          width: 100% !important;
          height: ${height}px !important;
          z-index: 2147483647 !important;
          display: flex !important;
          align-items: center !important;
          gap: 12px !important;
          box-sizing: border-box !important;
          padding: 8px 16px !important;
          background: #eef1f5 !important;
          border-bottom: 1px solid #cbd1d8 !important;
          color: #202124 !important;
          font: 14px/1.2 Arial, sans-serif !important;
        }
        #recording-browser-chrome .recording-browser-dots {
          color: #5f6368 !important;
          font-size: 20px !important;
          letter-spacing: 5px !important;
          white-space: nowrap !important;
        }
        #recording-browser-chrome .recording-browser-address {
          flex: 1 !important;
          overflow: hidden !important;
          padding: 9px 14px !important;
          border: 1px solid #d7dbe0 !important;
          border-radius: 18px !important;
          background: #fff !important;
          box-shadow: inset 0 1px 2px rgb(60 64 67 / 8%) !important;
          color: #3c4043 !important;
          text-overflow: ellipsis !important;
          white-space: nowrap !important;
        }
      `;
      document.documentElement.appendChild(style);

      const chrome = document.createElement("div");
      chrome.id = "recording-browser-chrome";
      chrome.setAttribute("aria-hidden", "true");
      chrome.innerHTML = `
        <span class="recording-browser-dots">‹ › ↻</span>
        <span class="recording-browser-address"></span>
      `;
      document.body.appendChild(chrome);

      const address = chrome.querySelector(".recording-browser-address") as HTMLElement;
      const updateAddress = () => {
        try {
          address.textContent = decodeURI(window.location.href);
        } catch {
          address.textContent = window.location.href;
        }
      };

      const originalPushState = history.pushState.bind(history);
      history.pushState = (data, unused, url) => {
        originalPushState(data, unused, url);
        queueMicrotask(updateAddress);
      };
      const originalReplaceState = history.replaceState.bind(history);
      history.replaceState = (data, unused, url) => {
        originalReplaceState(data, unused, url);
        queueMicrotask(updateAddress);
      };
      window.addEventListener("popstate", updateAddress);
      window.addEventListener("hashchange", updateAddress);
      updateAddress();

      const moveTopLayerBelowChrome = () => {
        for (const element of Array.from(document.body.querySelectorAll<HTMLElement>("*"))) {
          if (element === chrome || chrome.contains(element)) {
            continue;
          }
          const computed = getComputedStyle(element);
          if (
            (computed.position === "fixed" || computed.position === "sticky") &&
            computed.top === "0px" &&
            !element.dataset.recordingChromeOffset
          ) {
            element.style.setProperty("top", `${height}px`, "important");
            element.dataset.recordingChromeOffset = "true";
          }
        }
      };

      moveTopLayerBelowChrome();
      new MutationObserver(moveTopLayerBelowChrome).observe(document.body, {
        childList: true,
        subtree: true,
      });
    };

    if (document.readyState === "loading") {
      document.addEventListener("DOMContentLoaded", install, { once: true });
    } else {
      install();
    }
  }, { height: BROWSER_CHROME_HEIGHT });
}

type RecordClipOptions = {
  width?: number;
  height?: number;
  /** Show fake address bar so URL changes are visible (e.g. catalog filters). */
  addressBar?: boolean;
};

type RecordClipFixtures = {
  recordClip: (
    absolutePath: string,
    run: (page: Page) => Promise<void>,
    options?: RecordClipOptions,
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
      if (options?.addressBar) {
        await installAddressBar(page);
      }

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

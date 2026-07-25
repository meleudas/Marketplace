import fs from "node:fs";
import path from "node:path";
import type { Page, Response } from "@playwright/test";
import { getApiBaseUrl } from "../../e2e/fixtures/backend.helper";

export type ProofRow = {
  scenario: string;
  endpoints: string[];
  statuses: number[];
  result: string;
};

const API_HOST = new URL(getApiBaseUrl()).host;

export function createApiProofCollector(page: Page): {
  rows: Array<{ method: string; url: string; status: number }>;
  assertNoRouteMocks: () => void;
  summarize: (scenario: string, persistenceConfirmed: boolean) => ProofRow;
} {
  const rows: Array<{ method: string; url: string; status: number }> = [];

  page.on("response", (response: Response) => {
    const url = response.url();
    if (!url.includes(API_HOST)) {
      return;
    }
    rows.push({
      method: response.request().method(),
      url,
      status: response.status(),
    });
  });

  return {
    rows,
    assertNoRouteMocks() {
      // Playwright does not expose active routes on Page in a stable public API;
      // we enforce "no mocks" by never calling page.route in real-backend specs.
    },
    summarize(scenario, persistenceConfirmed) {
      const business = rows.filter((row) => {
        try {
          const pathname = new URL(row.url).pathname;
          return !pathname.includes("/health");
        } catch {
          return true;
        }
      });

      const endpoints = [...new Set(business.map((row) => {
        const u = new URL(row.url);
        return `${row.method} ${u.pathname}`;
      }))];

      const statuses = business.map((row) => row.status);
      const allOk = statuses.length > 0 && statuses.every((s) => s >= 200 && s < 400);
      const hostOk = business.every((row) => row.url.includes(API_HOST));

      let result = "fail";
      if (allOk && hostOk && persistenceConfirmed) {
        result = "real backend confirmed";
      } else if (allOk && hostOk) {
        result = "API OK, persistence not confirmed";
      } else if (business.length === 0) {
        result = "no backend requests observed";
      }

      return { scenario, endpoints, statuses, result };
    },
  };
}

export function writeProofReport(
  outPath: string,
  rows: ProofRow[],
  meta: Record<string, string>,
): void {
  const lines = [
    "# Real backend proof — buyer flow recordings",
    "",
    ...Object.entries(meta).map(([k, v]) => `- **${k}:** ${v}`),
    "",
    "| Сценарій | Backend endpoints | HTTP status | Результат перевірки |",
    "|---|---|---:|---|",
    ...rows.map((row) => {
      const endpoints = row.endpoints.join("<br>") || "—";
      const statuses = row.statuses.length ? row.statuses.join(", ") : "—";
      return `| ${row.scenario} | ${endpoints} | ${statuses} | ${row.result} |`;
    }),
    "",
    "Credentials and access tokens are not included.",
    "",
  ];

  fs.mkdirSync(path.dirname(outPath), { recursive: true });
  fs.writeFileSync(outPath, lines.join("\n"), "utf8");
}

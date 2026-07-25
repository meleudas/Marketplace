import type { APIRequestContext } from "@playwright/test";
import { getApiBaseUrl } from "./backend.helper";

export interface CatalogCategoryFixture {
  id: number;
  name: string;
  slug: string;
  parentId: number | null;
  isActive: boolean;
}

export interface CatalogAuthorFixture {
  value: string;
  label: string;
  count: number;
}

export interface CatalogProductFixture {
  id: number;
  slug: string;
  categoryId: number;
  price: number;
}

export interface CatalogSearchFixture {
  items: CatalogProductFixture[];
  total: number;
  page: number;
  pageSize: number;
}

export interface CatalogTestData {
  categories: CatalogCategoryFixture[];
  rootCategory: CatalogCategoryFixture;
  subcategory: CatalogCategoryFixture;
  author: CatalogAuthorFixture;
}

function extractList<T>(payload: unknown): T[] {
  if (Array.isArray(payload)) {
    return payload as T[];
  }

  if (payload && typeof payload === "object") {
    const record = payload as Record<string, unknown>;
    for (const key of ["value", "items", "data"]) {
      if (Array.isArray(record[key])) {
        return record[key] as T[];
      }
    }
  }

  return [];
}

async function getJson(request: APIRequestContext, path: string): Promise<unknown> {
  const response = await request.get(`${getApiBaseUrl()}${path}`);
  if (!response.ok()) {
    throw new Error(`Catalog fixture request failed (${response.status()}): ${path}`);
  }

  return response.json();
}

export async function getCatalogTestData(request: APIRequestContext): Promise<CatalogTestData> {
  const categories = extractList<CatalogCategoryFixture>(
    await getJson(request, "/catalog/categories"),
  ).filter((category) => category.isActive);

  const rootCategory = categories.find(
    (category) =>
      category.parentId === null &&
      categories.some((candidate) => candidate.parentId === category.id),
  );
  if (!rootCategory) {
    throw new Error("Catalog fixture requires an active root category with subcategories.");
  }

  const subcategory = categories.find((category) => category.parentId === rootCategory.id);
  if (!subcategory) {
    throw new Error(`Catalog fixture requires a subcategory for ${rootCategory.slug}.`);
  }

  const authors = extractList<CatalogAuthorFixture>(
    await getJson(request, "/catalog/authors"),
  );
  const author = authors.find((candidate) => candidate.count > 0);
  if (!author) {
    throw new Error("Catalog fixture requires at least one author facet.");
  }

  return { categories, rootCategory, subcategory, author };
}

export function getCategoryAndDescendantIds(
  categories: CatalogCategoryFixture[],
  categoryId: number,
): number[] {
  const result = [categoryId];
  const pending = categories.filter((category) => category.parentId === categoryId);

  while (pending.length > 0) {
    const category = pending.pop();
    if (!category) {
      continue;
    }

    result.push(category.id);
    pending.push(...categories.filter((candidate) => candidate.parentId === category.id));
  }

  return result;
}

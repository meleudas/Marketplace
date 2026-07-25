import {
  DEFAULT_CATALOG_PRODUCT_SORT,
  type CatalogProductSort,
} from "@/features/storefront/lib/catalog-product-sort";

export type CatalogQueryRecord = Record<string, string | string[] | undefined>;

export interface CatalogUrlParams {
  page: number;
  q: string;
  sort: CatalogProductSort | null;
  format: string[];
  authors: string[];
  categories: string[];
  minPrice: string;
  maxPrice: string;
  pageSize: number;
}

function pickParam(query: CatalogQueryRecord, key: string): string | null {
  const value = query[key];
  if (Array.isArray(value)) {
    return value[0] ?? null;
  }
  return value ?? null;
}

function parseCatalogSortParam(value: string | null): CatalogProductSort | null {
  if (
    value === "relevance" ||
    value === "newest" ||
    value === "price_asc" ||
    value === "price_desc"
  ) {
    return value;
  }

  return null;
}

export function parseCatalogQuery(query: CatalogQueryRecord = {}): CatalogUrlParams {
  const page = Math.max(1, parseInt(pickParam(query, "page") ?? "", 10) || 1);
  const q = pickParam(query, "q") ?? "";
  const sort = parseCatalogSortParam(pickParam(query, "sort"));
  const format = (pickParam(query, "format") ?? "").split(",").filter(Boolean);
  const authors = (pickParam(query, "authors") ?? "").split(",").filter(Boolean);
  const categories = (pickParam(query, "categories") ?? "").split(",").filter(Boolean);
  const minPrice = pickParam(query, "minPrice") ?? "";
  const maxPrice = pickParam(query, "maxPrice") ?? "";
  const perPage = parseInt(pickParam(query, "perPage") ?? "", 10);
  const pageSize = [10, 20, 30, 40, 50].includes(perPage) ? perPage : 20;

  return { page, q, sort, format, authors, categories, minPrice, maxPrice, pageSize };
}

export function buildCatalogUrl(
  categorySlug: string | undefined,
  state: {
    authors: string[];
    categories: string[];
    format: string[];
    minPrice: string;
    maxPrice: string;
    page: number;
    searchQuery: string;
    selectedSort: CatalogProductSort;
    pageSize: number;
  },
): string {
  const params = new URLSearchParams();

  if (state.authors.length > 0) {
    params.set("authors", state.authors.join(","));
  }
  if (state.categories.length > 0) {
    params.set("categories", state.categories.join(","));
  }
  if (state.format.length > 0) {
    params.set("format", state.format.join(","));
  }
  if (state.minPrice) {
    params.set("minPrice", state.minPrice);
  }
  if (state.maxPrice) {
    params.set("maxPrice", state.maxPrice);
  }
  if (state.page > 1) {
    params.set("page", String(state.page));
  }
  if (state.searchQuery) {
    params.set("q", state.searchQuery);
  }
  if (state.selectedSort !== DEFAULT_CATALOG_PRODUCT_SORT) {
    params.set("sort", state.selectedSort);
  }
  if (state.pageSize !== 20) {
    params.set("perPage", String(state.pageSize));
  }

  const qs = params.toString();
  const basePath = categorySlug ? `/catalog/${categorySlug}` : "/catalog";
  return qs ? `${basePath}?${qs}` : basePath;
}

export function getCatalogNavigationKey(
  categorySlug: string | undefined,
  query: CatalogQueryRecord,
): string {
  const parsed = parseCatalogQuery(query);
  return [
    categorySlug ?? "",
    parsed.format.join(","),
    parsed.categories.join(","),
    parsed.authors.join(","),
    parsed.minPrice,
    parsed.maxPrice,
    parsed.q,
    parsed.sort ?? "",
  ].join("::");
}

/** Update the address bar without App Router navigation. */
export function replaceCatalogUrlShallow(url: string): void {
  if (typeof window === "undefined") {
    return;
  }

  const currentUrl = `${window.location.pathname}${window.location.search}`;
  if (currentUrl === url) {
    return;
  }

  window.history.replaceState(window.history.state, "", url);
}


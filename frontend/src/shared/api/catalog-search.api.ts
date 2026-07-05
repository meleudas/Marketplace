import { apiClient } from "./http.client";

export interface CatalogSearchProductDto {
  id: number;
  slug: string;
  name: string;
  price: number;
  description?: string;
}

export interface CatalogSearchResultDto {
  items: CatalogSearchProductDto[];
}

export interface SearchCatalogProductsParams {
  query?: string;
  limit?: number;
}

const getCatalogProducts = async (): Promise<CatalogSearchProductDto[]> => {
  const response = await apiClient.get<unknown>("/catalog/products");
  const payload = response.data as
    | CatalogSearchProductDto[]
    | { value?: CatalogSearchProductDto[]; items?: CatalogSearchProductDto[]; data?: CatalogSearchProductDto[] };

  if (Array.isArray(payload)) {
    return payload;
  }

  if (payload && typeof payload === "object") {
    if (Array.isArray(payload.value)) {
      return payload.value;
    }

    if (Array.isArray(payload.items)) {
      return payload.items;
    }

    if (Array.isArray(payload.data)) {
      return payload.data;
    }
  }

  return [];
};

const normalize = (value: string): string => value.toLowerCase().trim();

const filterProductsLocally = (products: CatalogSearchProductDto[], query: string): CatalogSearchProductDto[] => {
  const normalizedQuery = normalize(query);

  if (!normalizedQuery) {
    return [];
  }

  return products.filter((product) =>
    [product.name, product.slug, product.description ?? ""].some((value) =>
      normalize(value).includes(normalizedQuery),
    ),
  );
};

export const searchCatalogProducts = async (
  params: SearchCatalogProductsParams,
): Promise<CatalogSearchResultDto> => {
  const searchParams = new URLSearchParams();
  const query = params.query?.trim() ?? "";

  if (query) {
    searchParams.set("query", query);
  }

  if (typeof params.limit === "number") {
    searchParams.set("pageSize", String(params.limit));
  }

  try {
    const searchQuery = searchParams.toString();
    const response = await apiClient.get<CatalogSearchResultDto>(
      searchQuery ? `/catalog/products/search?${searchQuery}` : "/catalog/products/search",
    );

    if (response.data.items.length > 0) {
      return response.data;
    }
  } catch {
    // Fallback to local search below when Elasticsearch is unavailable.
  }

  const products = await getCatalogProducts();
  const items = filterProductsLocally(products, query).slice(0, params.limit ?? 4).map((product) => ({
    id: product.id,
    slug: product.slug,
    name: product.name,
    price: product.price,
  }));

  return { items };
};

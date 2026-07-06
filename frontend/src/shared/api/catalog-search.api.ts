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

  const searchQuery = searchParams.toString();
  const response = await apiClient.get<CatalogSearchResultDto>(
    searchQuery ? `/catalog/products/search?${searchQuery}` : "/catalog/products/search",
  );

  return response.data;
};

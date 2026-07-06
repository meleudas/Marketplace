import { apiClient } from "@/shared/api/http.client";
import type {
  CatalogCategoryDto,
  CatalogCompanyDto,
  CatalogProductDetailDto,
  CatalogProductListItemDto,
  PersonalizedRecommendationsResultDto,
  ProductAvailabilityDto,
  ProductSearchResultDto,
} from "@/features/storefront/model/catalog.types";

const extractList = <T>(payload: unknown): T[] => {
  if (Array.isArray(payload)) {
    return payload as T[];
  }

  if (payload && typeof payload === "object") {
    const record = payload as Record<string, unknown>;

    if (Array.isArray(record.value)) {
      return record.value as T[];
    }

    if (Array.isArray(record.items)) {
      return record.items as T[];
    }

    if (Array.isArray(record.data)) {
      return record.data as T[];
    }
  }

  return [];
};

export const getCatalogCompanies = async (): Promise<CatalogCompanyDto[]> => {
  const response = await apiClient.get<unknown>("/catalog/companies");
  return extractList<CatalogCompanyDto>(response.data);
};

export const getCatalogCompanyBySlug = async (slug: string): Promise<CatalogCompanyDto | null> => {
  const companies = await getCatalogCompanies();
  return companies.find((company) => company.slug === slug) ?? null;
};

export const getCatalogCategories = async (): Promise<CatalogCategoryDto[]> => {
  const response = await apiClient.get<unknown>("/catalog/categories");
  return extractList<CatalogCategoryDto>(response.data);
};

export const getCatalogProducts = async (): Promise<CatalogProductListItemDto[]> => {
  const response = await apiClient.get<unknown>("/catalog/products");
  return extractList<CatalogProductListItemDto>(response.data);
};

export interface GetPersonalizedRecommendationsParams {
  limit?: number;
}

export const getPersonalizedRecommendations = async (
  params: GetPersonalizedRecommendationsParams = {},
): Promise<PersonalizedRecommendationsResultDto> => {
  const searchParams = new URLSearchParams();

  if (typeof params.limit === "number") {
    searchParams.set("limit", String(params.limit));
  }

  const query = searchParams.toString();
  const response = await apiClient.get<PersonalizedRecommendationsResultDto>(
    query ? `/catalog/recommendations/me?${query}` : "/catalog/recommendations/me",
  );

  return response.data;
};

export interface SearchCatalogProductsParams {
  query?: string;
  name?: string;
  categoryIds?: number[];
  companyId?: string;
  minPrice?: number;
  maxPrice?: number;
  availabilityStatus?: string;
  author?: string;
  format?: string;
  genre?: string;
  sort?: string;
  page?: number;
  pageSize?: number;
  searchAfter?: string;
}

export const searchCatalogProducts = async (
  params: SearchCatalogProductsParams,
): Promise<ProductSearchResultDto> => {
  const searchParams = new URLSearchParams();

  if (params.query) searchParams.set("query", params.query);
  if (params.name) searchParams.set("name", params.name);
  if (params.companyId) searchParams.set("companyId", params.companyId);
  if (typeof params.minPrice === "number") searchParams.set("minPrice", String(params.minPrice));
  if (typeof params.maxPrice === "number") searchParams.set("maxPrice", String(params.maxPrice));
  if (params.availabilityStatus) searchParams.set("availabilityStatus", params.availabilityStatus);
  if (params.author) searchParams.set("author", params.author);
  if (params.format) searchParams.set("format", params.format);
  if (params.genre) searchParams.set("genre", params.genre);
  if (params.sort) searchParams.set("sort", params.sort);
  if (typeof params.page === "number") searchParams.set("page", String(params.page));
  if (typeof params.pageSize === "number") searchParams.set("pageSize", String(params.pageSize));
  if (params.searchAfter) searchParams.set("searchAfter", params.searchAfter);

  for (const categoryId of params.categoryIds ?? []) {
    searchParams.append("categoryIds", String(categoryId));
  }

  const response = await apiClient.get<ProductSearchResultDto>(
    `/catalog/products/search?${searchParams.toString()}`,
  );

  return response.data;
};

export interface ListCatalogBrowsableProductsParams {
  categoryIds?: number[];
  companyId?: string;
  minPrice?: number;
  maxPrice?: number;
  availabilityStatus?: string;
  sort?: string;
  page?: number;
  pageSize?: number;
  searchAfter?: string;
}

export interface ListCatalogOnSaleProductsParams extends ListCatalogBrowsableProductsParams {
  minDiscountPercent?: number;
}

const buildBrowsableProductsSearchParams = (
  params: ListCatalogBrowsableProductsParams,
): URLSearchParams => {
  const searchParams = new URLSearchParams();

  if (params.companyId) searchParams.set("companyId", params.companyId);
  if (typeof params.minPrice === "number") searchParams.set("minPrice", String(params.minPrice));
  if (typeof params.maxPrice === "number") searchParams.set("maxPrice", String(params.maxPrice));
  if (params.availabilityStatus) searchParams.set("availabilityStatus", params.availabilityStatus);
  if (params.sort) searchParams.set("sort", params.sort);
  if (typeof params.page === "number") searchParams.set("page", String(params.page));
  if (typeof params.pageSize === "number") searchParams.set("pageSize", String(params.pageSize));
  if (params.searchAfter) searchParams.set("searchAfter", params.searchAfter);

  for (const categoryId of params.categoryIds ?? []) {
    searchParams.append("categoryIds", String(categoryId));
  }

  return searchParams;
};

const getCatalogBrowsableProducts = async (
  path: "popular" | "new" | "on-sale",
  params: ListCatalogBrowsableProductsParams = {},
): Promise<ProductSearchResultDto> => {
  const searchParams = buildBrowsableProductsSearchParams(params);
  const query = searchParams.toString();
  const response = await apiClient.get<ProductSearchResultDto>(
    query ? `/catalog/products/${path}?${query}` : `/catalog/products/${path}`,
  );

  return response.data;
};

export const getCatalogPopularProducts = async (
  params: ListCatalogBrowsableProductsParams = {},
): Promise<ProductSearchResultDto> => getCatalogBrowsableProducts("popular", params);

export const getCatalogNewProducts = async (
  params: ListCatalogBrowsableProductsParams = {},
): Promise<ProductSearchResultDto> => getCatalogBrowsableProducts("new", params);

export const getCatalogOnSaleProducts = async (
  params: ListCatalogOnSaleProductsParams = {},
): Promise<ProductSearchResultDto> => {
  const searchParams = buildBrowsableProductsSearchParams(params);

  if (typeof params.minDiscountPercent === "number") {
    searchParams.set("minDiscountPercent", String(params.minDiscountPercent));
  }

  const query = searchParams.toString();
  const response = await apiClient.get<ProductSearchResultDto>(
    query ? `/catalog/products/on-sale?${query}` : "/catalog/products/on-sale",
  );

  return response.data;
};

export const getCatalogProductBySlug = async (slug: string): Promise<CatalogProductDetailDto> => {
  const response = await apiClient.get<CatalogProductDetailDto>(`/catalog/products/${slug}`);
  return response.data;
};

export const getProductAvailability = async (
  companyId: string,
  productId: string,
): Promise<ProductAvailabilityDto> => {
  const response = await apiClient.get<ProductAvailabilityDto>(
    `/catalog/companies/${companyId}/products/${productId}/availability`,
  );
  return response.data;
};

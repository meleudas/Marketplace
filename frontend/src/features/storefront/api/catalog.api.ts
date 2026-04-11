import { apiClient } from "@/shared/api/http.client";
import type {
  CatalogCategoryDto,
  CatalogCompanyDto,
  CatalogProductDetailDto,
  CatalogProductListItemDto,
  ProductAvailabilityDto,
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

export const getCatalogCategories = async (): Promise<CatalogCategoryDto[]> => {
  const response = await apiClient.get<unknown>("/catalog/categories");
  return extractList<CatalogCategoryDto>(response.data);
};

export const getCatalogProducts = async (): Promise<CatalogProductListItemDto[]> => {
  const response = await apiClient.get<unknown>("/catalog/products");
  return extractList<CatalogProductListItemDto>(response.data);
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

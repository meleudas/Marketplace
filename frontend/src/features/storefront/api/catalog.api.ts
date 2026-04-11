import { apiClient } from "@/shared/api/http.client";
import type {
  CatalogCategoryDto,
  CatalogCompanyDto,
  CatalogProductDetailDto,
  CatalogProductListItemDto,
  ProductAvailabilityDto,
} from "@/features/storefront/model/catalog.types";

export const getCatalogCompanies = async (): Promise<CatalogCompanyDto[]> => {
  const response = await apiClient.get<CatalogCompanyDto[]>("/catalog/companies");
  return response.data;
};

export const getCatalogCategories = async (): Promise<CatalogCategoryDto[]> => {
  const response = await apiClient.get<CatalogCategoryDto[]>("/catalog/categories");
  return response.data;
};

export const getCatalogProducts = async (): Promise<CatalogProductListItemDto[]> => {
  const response = await apiClient.get<CatalogProductListItemDto[]>("/catalog/products");
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


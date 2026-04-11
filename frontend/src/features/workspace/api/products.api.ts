import { apiClient } from "@/shared/api/http.client";
import type {
  CompanyProductDto,
  UpsertProductRequest,
  WorkspaceCategoryDto,
} from "@/features/workspace/model/workspace.types";

const extractList = <T>(payload: unknown): T[] => {
  if (Array.isArray(payload)) {
    return payload as T[];
  }

  if (payload && typeof payload === "object") {
    const data = payload as Record<string, unknown>;

    if (Array.isArray(data.value)) {
      return data.value as T[];
    }

    if (Array.isArray(data.items)) {
      return data.items as T[];
    }

    if (Array.isArray(data.data)) {
      return data.data as T[];
    }
  }

  return [];
};

export const getWorkspaceProducts = async (companyId: string): Promise<CompanyProductDto[]> => {
  const response = await apiClient.get<unknown>(`/companies/${companyId}/products`);
  return extractList<CompanyProductDto>(response.data);
};

interface ProductDtoResponse {
  product: CompanyProductDto;
  detail: unknown;
  images: unknown[];
}

export const createWorkspaceProduct = async (
  companyId: string,
  payload: UpsertProductRequest,
): Promise<CompanyProductDto> => {
  const response = await apiClient.post<ProductDtoResponse>(`/companies/${companyId}/products`, payload);
  return response.data.product;
};

export const updateWorkspaceProduct = async (
  companyId: string,
  id: number,
  payload: UpsertProductRequest,
): Promise<CompanyProductDto> => {
  const response = await apiClient.put<ProductDtoResponse>(`/companies/${companyId}/products/${id}`, payload);
  return response.data.product;
};

export const deleteWorkspaceProduct = async (companyId: string, id: number): Promise<void> => {
  await apiClient.delete(`/companies/${companyId}/products/${id}`);
};

export const getWorkspaceCategories = async (): Promise<WorkspaceCategoryDto[]> => {
  const response = await apiClient.get<unknown>("/catalog/categories");
  return extractList<WorkspaceCategoryDto>(response.data);
};

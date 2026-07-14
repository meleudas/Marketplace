import { apiClient } from "./http.client";

export interface CatalogCategoryNavDto {
  id: number;
  name: string;
  slug: string;
  parentId: number | null;
  sortOrder: number;
  isActive: boolean;
}

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

export const getCatalogCategories = async (): Promise<CatalogCategoryNavDto[]> => {
  const response = await apiClient.get<unknown>("/catalog/categories");
  return extractList<CatalogCategoryNavDto>(response.data);
};

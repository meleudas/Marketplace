import { apiClient } from "@/shared/api/http.client";
import type {
  CompanyMemberDto,
  CompanyMemberRoleRequest,
  CompanyMembershipDto,
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

export const getCompanyMembers = async (companyId: string): Promise<CompanyMemberDto[]> => {
  const response = await apiClient.get<unknown>(`/companies/${companyId}/members`);
  return extractList<CompanyMemberDto>(response.data);
};

export const getCompanyMembershipMe = async (companyId: string): Promise<CompanyMembershipDto> => {
  const response = await apiClient.get<CompanyMembershipDto>(`/companies/${companyId}/members/me`);
  return response.data;
};

export const assignCompanyMemberRole = async (
  companyId: string,
  userId: string,
  payload: CompanyMemberRoleRequest,
): Promise<void> => {
  await apiClient.post(`/companies/${companyId}/members/${userId}/role`, payload);
};

export const changeCompanyMemberRole = async (
  companyId: string,
  userId: string,
  payload: CompanyMemberRoleRequest,
): Promise<void> => {
  await apiClient.patch(`/companies/${companyId}/members/${userId}/role`, payload);
};

export const removeCompanyMember = async (companyId: string, userId: string): Promise<void> => {
  await apiClient.delete(`/companies/${companyId}/members/${userId}`);
};


import { AxiosError } from "axios";
import { apiClient } from "@/shared/api/http.client";
import {
  CompanyMembershipDto,
  WorkspaceMembershipError,
} from "@/features/workspace/model/workspace.types";

export const getMyCompanyMembership = async (companyId: string): Promise<CompanyMembershipDto> => {
  try {
    const response = await apiClient.get<CompanyMembershipDto>(`/companies/${companyId}/members/me`);
    return response.data;
  } catch (error) {
    const axiosError = error as AxiosError;

    if (axiosError.response?.status === 403) {
      throw new WorkspaceMembershipError("forbidden", "Forbidden");
    }

    if (axiosError.response?.status === 404) {
      throw new WorkspaceMembershipError("notFound", "Membership not found");
    }

    throw new WorkspaceMembershipError("unknown", "Failed to load workspace membership");
  }
};


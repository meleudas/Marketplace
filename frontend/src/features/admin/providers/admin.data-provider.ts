import type {
  BaseRecord,
  CreateParams,
  CreateResponse,
  DataProvider,
  DeleteOneParams,
  DeleteOneResponse,
  GetListParams,
  GetListResponse,
  GetOneParams,
  GetOneResponse,
  UpdateParams,
  UpdateResponse,
} from "@refinedev/core";
import { apiClient } from "@/shared/api/http.client";
import type {
  CategoryDto,
  CompanyDto,
  CreateCategoryRequest,
  CreateCompanyRequest,
  UpdateCategoryRequest,
  UpdateCompanyRequest,
} from "@/features/admin/model/admin.types";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";

type AdminResource = "companies" | "companies-pending" | "categories" | "categories-active";

const isAdminResource = (resource: string): resource is AdminResource => {
  return (
    resource === "companies" ||
    resource === "companies-pending" ||
    resource === "categories" ||
    resource === "categories-active"
  );
};

const getListEndpoint = (resource: AdminResource): string => {
  if (resource === "companies") {
    return "/admin/companies";
  }

  if (resource === "companies-pending") {
    return "/admin/companies/pending";
  }

  if (resource === "categories") {
    return "/admin/categories";
  }

  return "/admin/categories/active";
};

const listByResource = async (resource: AdminResource): Promise<CompanyDto[] | CategoryDto[]> => {
  const response = await apiClient.get<CompanyDto[] | CategoryDto[]>(getListEndpoint(resource));
  return response.data;
};

const getSingleFromList = async (params: GetOneParams) => {
  if (!isAdminResource(params.resource)) {
    throw new Error(`Unsupported resource: ${params.resource}`);
  }

  if (params.resource === "companies" || params.resource === "companies-pending") {
    const data = (await listByResource("companies")) as CompanyDto[];
    const item = data.find((company) => company.id === String(params.id));

    if (!item) {
      throw new Error("Company not found");
    }

    return item;
  }

  const data = (await listByResource("categories")) as CategoryDto[];
  const targetId = Number(params.id);
  const item = data.find((category) => category.id === targetId);

  if (!item) {
    throw new Error("Category not found");
  }

  return item;
};

export const adminDataProvider: DataProvider = {
  getApiUrl: () => API_URL,

  getList: async <TData extends BaseRecord = BaseRecord>(
    params: GetListParams,
  ): Promise<GetListResponse<TData>> => {
    if (!isAdminResource(params.resource)) {
      throw new Error(`Unsupported resource: ${params.resource}`);
    }

    const data = await listByResource(params.resource);

    return {
      data: data as unknown as TData[],
      total: data.length,
    };
  },

  getOne: async <TData extends BaseRecord = BaseRecord>(
    params: GetOneParams,
  ): Promise<GetOneResponse<TData>> => {
    const data = await getSingleFromList(params);

    return {
      data: data as unknown as TData,
    };
  },

  create: async <TData extends BaseRecord = BaseRecord, TVariables = unknown>(
    params: CreateParams<TVariables>,
  ): Promise<CreateResponse<TData>> => {
    if (params.resource === "companies") {
      const response = await apiClient.post<CompanyDto>(
        "/admin/companies",
        params.variables as CreateCompanyRequest,
      );

      return { data: response.data as unknown as TData };
    }

    if (params.resource === "categories") {
      const response = await apiClient.post<CategoryDto>(
        "/admin/categories",
        params.variables as CreateCategoryRequest,
      );

      return { data: response.data as unknown as TData };
    }

    throw new Error(`Create is unsupported for resource: ${params.resource}`);
  },

  update: async <TData extends BaseRecord = BaseRecord, TVariables = unknown>(
    params: UpdateParams<TVariables>,
  ): Promise<UpdateResponse<TData>> => {
    if (params.resource === "companies") {
      const response = await apiClient.put<CompanyDto>(
        `/admin/companies/${params.id}`,
        params.variables as UpdateCompanyRequest,
      );

      return { data: response.data as unknown as TData };
    }

    if (params.resource === "categories") {
      const response = await apiClient.put<CategoryDto>(
        `/admin/categories/${params.id}`,
        params.variables as UpdateCategoryRequest,
      );

      return { data: response.data as unknown as TData };
    }

    throw new Error(`Update is unsupported for resource: ${params.resource}`);
  },

  deleteOne: async <TData extends BaseRecord = BaseRecord, TVariables = unknown>(
    params: DeleteOneParams<TVariables>,
  ): Promise<DeleteOneResponse<TData>> => {
    const baseResource =
      params.resource === "companies" || params.resource === "companies-pending"
        ? "companies"
        : "categories";
    await apiClient.delete(`/admin/${baseResource}/${params.id}`);

    return {
      data: {
        id: params.id,
      } as unknown as TData,
    };
  },

  custom: async (params) => {
    const method = params.method ?? "get";
    const response = await apiClient.request({
      url: params.url,
      method,
      data: params.payload,
      params: params.query,
      headers: params.headers,
    });

    return {
      data: response.data,
    };
  },
};




export interface CompanyAddressDto {
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
}

export interface CompanyDto {
  id: string;
  name: string;
  slug: string;
  description: string;
  imageUrl: string | null;
  contactEmail: string;
  contactPhone: string;
  address: CompanyAddressDto;
  isApproved: boolean;
  approvedAt: string | null;
  approvedByUserId: string | null;
  rating: number | null;
  reviewCount: number;
  followerCount: number;
  metaRaw: string | null;
  createdAt: string;
  updatedAt: string;
  isDeleted: boolean;
  deletedAt: string | null;
}

export interface CreateCompanyRequest {
  name: string;
  slug: string;
  description: string;
  imageUrl: string | null;
  contactEmail: string;
  contactPhone: string;
  address: CompanyAddressDto;
  metaRaw: string | null;
}

export type UpdateCompanyRequest = CreateCompanyRequest;

export interface CategoryDto {
  id: number;
  name: string;
  slug: string;
  imageUrl: string | null;
  parentId: number | null;
  description: string | null;
  metaRaw: string | null;
  sortOrder: number;
  isActive: boolean;
  productCount: number;
  createdAt: string;
  updatedAt: string;
  isDeleted: boolean;
  deletedAt: string | null;
}

export interface CreateCategoryRequest {
  name: string;
  slug: string;
  imageUrl: string | null;
  parentCategoryId: number | null;
  description: string | null;
  metaRaw: string | null;
  sortOrder: number;
  isActive: boolean;
}

export interface UpdateCategoryRequest {
  name: string;
  slug: string;
  imageUrl: string | null;
  parentCategoryId: number | null;
  description: string | null;
  metaRaw: string | null;
  sortOrder: number;
}


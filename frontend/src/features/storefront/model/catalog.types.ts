export type AvailabilityStatus = "in_stock" | "low_stock" | "out_of_stock" | string;

export interface CatalogCategoryDto {
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

export interface CatalogCompanyAddressDto {
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
}

export interface CatalogCompanyDto {
  id: string;
  name: string;
  slug: string;
  description: string;
  imageUrl: string | null;
  contactEmail: string;
  contactPhone: string;
  address: CatalogCompanyAddressDto;
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

export interface CatalogProductListItemDto {
  id: string;
  companyId: string;
  categoryId: number | null;
  name: string;
  slug: string;
  description?: string | null;
  imageUrl?: string | null;
  price?: number | null;
  oldPrice?: number | null;
  companyName?: string | null;
  categoryName?: string | null;
  categorySlug?: string | null;
  availableQty: number;
  availabilityStatus: AvailabilityStatus;
}

export interface CatalogProductDetailDto {
  product: {
    id: string;
    companyId: string;
    categoryId: number | null;
    name: string;
    slug: string;
    description?: string | null;
    imageUrl?: string | null;
    price?: number | null;
    oldPrice?: number | null;
    availableQty?: number;
    availabilityStatus?: AvailabilityStatus;
  };
  detail: {
    description?: string | null;
  } | null;
  images: Array<{
    id?: string;
    url: string;
    sortOrder?: number;
    altText?: string | null;
  }>;
  availableQty?: number;
  availabilityStatus?: AvailabilityStatus;
}

export interface ProductAvailabilityDto {
  availableQty: number;
  availabilityStatus: AvailabilityStatus;
}


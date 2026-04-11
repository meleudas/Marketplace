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
  id: number;
  companyId: string;
  name: string;
  slug: string;
  description: string;
  price: number;
  oldPrice: number | null;
  categoryId: number;
  status: string;
  hasVariants: boolean;
  stock: number;
  minStock: number;
  availableQty: number;
  availabilityStatus: AvailabilityStatus;
  createdAt: string;
  updatedAt: string;
}

export interface CatalogProductImageDto {
  imageUrl: string;
  thumbnailUrl: string;
  altText: string;
  sortOrder: number;
  isMain: boolean;
  width: number | null;
  height: number | null;
  fileSize: number | null;
}

export interface CatalogProductDetailDto {
  product: CatalogProductListItemDto;
  detail: {
    slug: string;
    attributesRaw: string | null;
    variantsRaw: string | null;
    specificationsRaw: string | null;
    seoRaw: string | null;
    contentBlocksRaw: string | null;
    tags: string[];
    brands: string[];
  } | null;
  images: CatalogProductImageDto[];
}

export interface ProductAvailabilityDto {
  productId: number;
  availableQty: number;
  availabilityStatus: AvailabilityStatus;
}


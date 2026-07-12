import { apiClient } from "@/shared/api/http.client";

export interface ReviewReplyDto {
  id: number;
  companyId: string;
  authorUserId: string;
  body: string;
  isEdited: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface ProductReviewDto {
  id: number;
  targetType: string;
  productId: number | null;
  companyId: string | null;
  userId: string;
  userName: string;
  rating: number | null;
  overallRating: number | null;
  title: string | null;
  comment: string;
  isVerifiedPurchase: boolean;
  status: number;
  createdAt: string;
  updatedAt: string;
  reply: ReviewReplyDto | null;
}

export interface ProductReviewListDto {
  page: number;
  size: number;
  items: ProductReviewDto[];
}

export interface FetchProductReviewsParams {
  page?: number;
  size?: number;
}

export const fetchProductReviews = async (
  productId: number,
  params: FetchProductReviewsParams = {},
): Promise<ProductReviewListDto> => {
  const searchParams = new URLSearchParams();

  if (typeof params.page === "number") searchParams.set("page", String(params.page));
  if (typeof params.size === "number") searchParams.set("size", String(params.size));

  const query = searchParams.toString();
  const response = await apiClient.get<ProductReviewListDto>(
    query ? `/products/${productId}/reviews?${query}` : `/products/${productId}/reviews`,
  );

  return response.data;
};

export interface CreateProductReviewPayload {
  rating: number;
  title?: string | null;
  comment: string;
}

export const createProductReview = async (
  productId: number,
  payload: CreateProductReviewPayload,
): Promise<ProductReviewDto> => {
  const response = await apiClient.post<ProductReviewDto>(`/products/${productId}/reviews`, payload);
  return response.data;
};

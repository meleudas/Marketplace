import type {
  CategoryDto,
  CreateCategoryRequest,
  UpdateCategoryRequest,
} from "@/features/admin/model/admin.types";
import type { CategoryFormValues } from "@/features/admin/validation/category-form.schema";

export const categoryDtoToFormValues = (category: CategoryDto): CategoryFormValues => {
  return {
    name: category.name,
    slug: category.slug,
    imageUrl: category.imageUrl ?? "",
    parentCategoryId: category.parentId,
    description: category.description ?? "",
    metaRaw: category.metaRaw ?? "",
    sortOrder: category.sortOrder,
    isActive: category.isActive,
  };
};

export const buildCreateCategoryPayload = (form: CategoryFormValues): CreateCategoryRequest => {
  const imageUrl = (form.imageUrl ?? "").trim();
  const description = (form.description ?? "").trim();
  const metaRaw = (form.metaRaw ?? "").trim();

  return {
    name: form.name.trim(),
    slug: form.slug.trim(),
    imageUrl: imageUrl || null,
    parentCategoryId: form.parentCategoryId,
    description: description || null,
    metaRaw: metaRaw || null,
    sortOrder: form.sortOrder,
    isActive: form.isActive,
  };
};

export const buildUpdateCategoryPayload = (form: CategoryFormValues): UpdateCategoryRequest => {
  const imageUrl = (form.imageUrl ?? "").trim();
  const description = (form.description ?? "").trim();
  const metaRaw = (form.metaRaw ?? "").trim();

  return {
    name: form.name.trim(),
    slug: form.slug.trim(),
    imageUrl: imageUrl || null,
    parentCategoryId: form.parentCategoryId,
    description: description || null,
    metaRaw: metaRaw || null,
    sortOrder: form.sortOrder,
  };
};


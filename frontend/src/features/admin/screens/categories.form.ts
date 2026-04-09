import type {
  CategoryDto,
  CreateCategoryRequest,
  UpdateCategoryRequest,
} from "@/features/admin/model/admin.types";

export interface CategoryFormState {
  name: string;
  slug: string;
  imageUrl: string;
  parentCategoryId: string;
  description: string;
  metaRaw: string;
  sortOrder: string;
  isActive: boolean;
}

export const defaultCategoryFormState: CategoryFormState = {
  name: "",
  slug: "",
  imageUrl: "",
  parentCategoryId: "",
  description: "",
  metaRaw: "",
  sortOrder: "0",
  isActive: true,
};

export const categoryDtoToFormState = (category: CategoryDto): CategoryFormState => {
  return {
    name: category.name,
    slug: category.slug,
    imageUrl: category.imageUrl ?? "",
    parentCategoryId: category.parentId === null ? "" : String(category.parentId),
    description: category.description ?? "",
    metaRaw: category.metaRaw ?? "",
    sortOrder: String(category.sortOrder),
    isActive: category.isActive,
  };
};

const parseOptionalNumber = (value: string): number | null => {
  const trimmed = value.trim();

  if (!trimmed) {
    return null;
  }

  const parsed = Number(trimmed);
  return Number.isFinite(parsed) ? parsed : null;
};

export const buildCreateCategoryPayload = (form: CategoryFormState): CreateCategoryRequest => {
  return {
    name: form.name.trim(),
    slug: form.slug.trim(),
    imageUrl: form.imageUrl.trim() || null,
    parentCategoryId: parseOptionalNumber(form.parentCategoryId),
    description: form.description.trim() || null,
    metaRaw: form.metaRaw.trim() || null,
    sortOrder: Number(form.sortOrder) || 0,
    isActive: form.isActive,
  };
};

export const buildUpdateCategoryPayload = (form: CategoryFormState): UpdateCategoryRequest => {
  return {
    name: form.name.trim(),
    slug: form.slug.trim(),
    imageUrl: form.imageUrl.trim() || null,
    parentCategoryId: parseOptionalNumber(form.parentCategoryId),
    description: form.description.trim() || null,
    metaRaw: form.metaRaw.trim() || null,
    sortOrder: Number(form.sortOrder) || 0,
  };
};


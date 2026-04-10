import { z } from "zod";

export const categoryFormSchema = z.object({
  name: z.string().trim().min(1, "Name is required"),
  slug: z.string().trim().min(1, "Slug is required"),
  imageUrl: z.string().nullable().optional(),
  parentCategoryId: z.number().nullable(),
  description: z.string().nullable().optional(),
  metaRaw: z.string().nullable().optional(),
  sortOrder: z.number({ error: "Sort order is required" }),
  isActive: z.boolean(),
});

export type CategoryFormValues = z.infer<typeof categoryFormSchema>;

export const defaultCategoryFormValues: CategoryFormValues = {
  name: "",
  slug: "",
  imageUrl: "",
  parentCategoryId: null,
  description: "",
  metaRaw: "",
  sortOrder: 0,
  isActive: true,
};


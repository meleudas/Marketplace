import { z } from "zod";

export const productFormSchema = z.object({
  name: z.string().trim().min(1, "Name is required"),
  slug: z.string().trim().min(1, "Slug is required"),
  description: z.string().trim().min(1, "Description is required"),
  price: z.number().min(0, "Price must be >= 0"),
  oldPrice: z.string().trim().optional(),
  minStock: z.number().int().min(0, "Min stock must be >= 0"),
  categoryId: z.number().int().positive("Category is required"),
  hasVariants: z.boolean(),
  detailJson: z.string().trim().default("{}"),
  imagesJson: z.string().trim().default("[]"),
});

export type ProductFormValues = z.infer<typeof productFormSchema>;

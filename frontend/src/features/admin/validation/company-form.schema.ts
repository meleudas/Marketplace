import { z } from "zod";

export const companyFormSchema = z.object({
  name: z.string().trim().min(1, "Name is required"),
  slug: z.string().trim().min(1, "Slug is required"),
  description: z.string().trim().min(1, "Description is required"),
  imageUrl: z.string().nullable().optional(),
  contactEmail: z.string().trim().min(1, "Contact email is required").email("Enter a valid email"),
  contactPhone: z.string().trim().min(1, "Contact phone is required"),
  address: z.object({
    street: z.string().trim().min(1, "Street is required"),
    city: z.string().trim().min(1, "City is required"),
    state: z.string().trim().min(1, "State is required"),
    postalCode: z.string().trim().min(1, "Postal code is required"),
    country: z.string().trim().min(1, "Country is required"),
  }),
  metaRaw: z.string().nullable().optional(),
});

export type CompanyFormValues = z.infer<typeof companyFormSchema>;

export const defaultCompanyFormValues: CompanyFormValues = {
  name: "",
  slug: "",
  description: "",
  imageUrl: "",
  contactEmail: "",
  contactPhone: "",
  address: {
    street: "",
    city: "",
    state: "",
    postalCode: "",
    country: "",
  },
  metaRaw: "",
};


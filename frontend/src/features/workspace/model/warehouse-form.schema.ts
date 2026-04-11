import { z } from "zod";

export const warehouseFormSchema = z.object({
  name: z.string().trim().min(1, "Warehouse name is required"),
  code: z.string().trim().optional(),
  street: z.string().trim().min(1, "Street is required"),
  city: z.string().trim().min(1, "City is required"),
  state: z.string().trim().min(1, "State is required"),
  postalCode: z.string().trim().min(1, "Postal code is required"),
  country: z.string().trim().min(1, "Country is required"),
  timeZone: z.string().trim().min(1, "Time zone is required"),
  priority: z.number().int().min(0, "Priority must be >= 0"),
});

export type WarehouseFormValues = z.infer<typeof warehouseFormSchema>;

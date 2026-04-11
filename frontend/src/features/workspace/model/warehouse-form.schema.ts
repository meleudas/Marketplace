import { z } from "zod";

export const warehouseFormSchema = z.object({
  name: z.string().trim().min(1, "Warehouse name is required"),
  code: z.string().trim().optional(),
  address: z.string().trim().optional(),
});

export type WarehouseFormValues = z.infer<typeof warehouseFormSchema>;


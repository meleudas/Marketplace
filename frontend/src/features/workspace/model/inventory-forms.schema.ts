import { z } from "zod";

export const receiveFormSchema = z.object({
  warehouseId: z.string().trim().min(1, "Warehouse is required"),
  productId: z.string().trim().min(1, "Product ID is required"),
  quantity: z.number().int().positive("Quantity must be greater than 0"),
  note: z.string().trim().optional(),
});

export const shipFormSchema = z.object({
  warehouseId: z.string().trim().min(1, "Warehouse is required"),
  productId: z.string().trim().min(1, "Product ID is required"),
  quantity: z.number().int().positive("Quantity must be greater than 0"),
  note: z.string().trim().optional(),
});

export const adjustFormSchema = z.object({
  warehouseId: z.string().trim().min(1, "Warehouse is required"),
  productId: z.string().trim().min(1, "Product ID is required"),
  quantityDelta: z.number().int(),
  reason: z.string().trim().optional(),
});

export const transferFormSchema = z.object({
  fromWarehouseId: z.string().trim().min(1, "From warehouse is required"),
  toWarehouseId: z.string().trim().min(1, "To warehouse is required"),
  productId: z.string().trim().min(1, "Product ID is required"),
  quantity: z.number().int().positive("Quantity must be greater than 0"),
  note: z.string().trim().optional(),
});

export const reserveFormSchema = z.object({
  warehouseId: z.string().trim().min(1, "Warehouse is required"),
  productId: z.string().trim().min(1, "Product ID is required"),
  quantity: z.number().int().positive("Quantity must be greater than 0"),
  note: z.string().trim().optional(),
});

export const releaseReservationFormSchema = z.object({
  reservationCode: z.string().trim().min(1, "Reservation code is required"),
});

export type ReceiveFormValues = z.infer<typeof receiveFormSchema>;
export type ShipFormValues = z.infer<typeof shipFormSchema>;
export type AdjustFormValues = z.infer<typeof adjustFormSchema>;
export type TransferFormValues = z.infer<typeof transferFormSchema>;
export type ReserveFormValues = z.infer<typeof reserveFormSchema>;
export type ReleaseReservationFormValues = z.infer<typeof releaseReservationFormSchema>;

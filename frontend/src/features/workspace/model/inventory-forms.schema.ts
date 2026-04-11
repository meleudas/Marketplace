import { z } from "zod";

export const receiveFormSchema = z.object({
  warehouseId: z.number().int().positive("Warehouse is required"),
  productId: z.number().int().positive("Product ID is required"),
  quantity: z.number().int().positive("Quantity must be greater than 0"),
  reference: z.string().trim().optional(),
});

export const shipFormSchema = z.object({
  warehouseId: z.number().int().positive("Warehouse is required"),
  productId: z.number().int().positive("Product ID is required"),
  quantity: z.number().int().positive("Quantity must be greater than 0"),
  reference: z.string().trim().optional(),
});

export const adjustFormSchema = z.object({
  warehouseId: z.number().int().positive("Warehouse is required"),
  productId: z.number().int().positive("Product ID is required"),
  onHand: z.number().int().min(0, "On hand must be >= 0"),
  reserved: z.number().int().min(0, "Reserved must be >= 0"),
  reorderPoint: z.number().int().min(0, "Reorder point must be >= 0"),
  reason: z.string().trim().optional(),
});

export const transferFormSchema = z.object({
  fromWarehouseId: z.number().int().positive("From warehouse is required"),
  toWarehouseId: z.number().int().positive("To warehouse is required"),
  productId: z.number().int().positive("Product ID is required"),
  quantity: z.number().int().positive("Quantity must be greater than 0"),
});

export const reserveFormSchema = z.object({
  warehouseId: z.number().int().positive("Warehouse is required"),
  productId: z.number().int().positive("Product ID is required"),
  quantity: z.number().int().positive("Quantity must be greater than 0"),
  ttlMinutes: z.number().int().min(1).max(120),
  reference: z.string().trim().optional(),
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

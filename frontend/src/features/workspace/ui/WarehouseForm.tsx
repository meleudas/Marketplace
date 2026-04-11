"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { warehouseFormSchema, type WarehouseFormValues } from "@/features/workspace/model/warehouse-form.schema";
import type { CreateWarehouseRequest, WarehouseDto } from "@/features/workspace/model/workspace.types";
import styles from "@/features/workspace/screens/WorkspaceScreen.module.css";

interface WarehouseFormProps {
  initialWarehouse?: WarehouseDto | null;
  busy: boolean;
  submitLabel: string;
  onSubmit: (payload: CreateWarehouseRequest) => Promise<void>;
}

export function WarehouseForm({ initialWarehouse, busy, submitLabel, onSubmit }: WarehouseFormProps) {
  const form = useForm<z.input<typeof warehouseFormSchema>, unknown, WarehouseFormValues>({
    resolver: zodResolver(warehouseFormSchema),
    defaultValues: {
      name: initialWarehouse?.name ?? "",
      code: initialWarehouse?.code ?? "",
      address: initialWarehouse?.address ?? "",
    },
  });

  return (
    <form
      className={styles.formGrid}
      onSubmit={form.handleSubmit(async (values) => {
        await onSubmit({
          name: values.name,
          code: values.code?.trim() ? values.code : null,
          address: values.address?.trim() ? values.address : null,
        });
      })}
    >
      <label className={styles.field}>
        Name
        <input className={styles.input} {...form.register("name")} />
        {form.formState.errors.name ? <span className={styles.error}>{form.formState.errors.name.message}</span> : null}
      </label>

      <label className={styles.field}>
        Code
        <input className={styles.input} {...form.register("code")} />
      </label>

      <label className={styles.field}>
        Address
        <input className={styles.input} {...form.register("address")} />
      </label>

      <button type="submit" className={styles.primaryButton} disabled={busy || form.formState.isSubmitting}>
        {busy || form.formState.isSubmitting ? "Saving..." : submitLabel}
      </button>
    </form>
  );
}

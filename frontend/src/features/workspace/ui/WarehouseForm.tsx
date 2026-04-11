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
      street: initialWarehouse?.street ?? "",
      city: initialWarehouse?.city ?? "",
      state: initialWarehouse?.state ?? "",
      postalCode: initialWarehouse?.postalCode ?? "",
      country: initialWarehouse?.country ?? "",
      timeZone: initialWarehouse?.timeZone ?? "Europe/Kyiv",
      priority: initialWarehouse?.priority ?? 0,
    },
  });

  return (
    <form
      className={styles.formGrid}
      onSubmit={form.handleSubmit(async (values) => {
        await onSubmit({
          name: values.name,
          code: values.code?.trim() || "",
          street: values.street,
          city: values.city,
          state: values.state,
          postalCode: values.postalCode,
          country: values.country,
          timeZone: values.timeZone,
          priority: values.priority,
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
        Street
        <input className={styles.input} {...form.register("street")} />
        {form.formState.errors.street ? <span className={styles.error}>{form.formState.errors.street.message}</span> : null}
      </label>

      <label className={styles.field}>
        City
        <input className={styles.input} {...form.register("city")} />
        {form.formState.errors.city ? <span className={styles.error}>{form.formState.errors.city.message}</span> : null}
      </label>

      <label className={styles.field}>
        State
        <input className={styles.input} {...form.register("state")} />
        {form.formState.errors.state ? <span className={styles.error}>{form.formState.errors.state.message}</span> : null}
      </label>

      <label className={styles.field}>
        Postal code
        <input className={styles.input} {...form.register("postalCode")} />
        {form.formState.errors.postalCode ? (
          <span className={styles.error}>{form.formState.errors.postalCode.message}</span>
        ) : null}
      </label>

      <label className={styles.field}>
        Country
        <input className={styles.input} {...form.register("country")} />
        {form.formState.errors.country ? <span className={styles.error}>{form.formState.errors.country.message}</span> : null}
      </label>

      <label className={styles.field}>
        Time zone
        <input className={styles.input} {...form.register("timeZone")} />
        {form.formState.errors.timeZone ? (
          <span className={styles.error}>{form.formState.errors.timeZone.message}</span>
        ) : null}
      </label>

      <label className={styles.field}>
        Priority
        <input type="number" className={styles.input} {...form.register("priority", { valueAsNumber: true })} />
        {form.formState.errors.priority ? (
          <span className={styles.error}>{form.formState.errors.priority.message}</span>
        ) : null}
      </label>

      <button type="submit" className={styles.primaryButton} disabled={busy || form.formState.isSubmitting}>
        {busy || form.formState.isSubmitting ? "Saving..." : submitLabel}
      </button>
    </form>
  );
}

"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { productFormSchema, type ProductFormValues } from "@/features/workspace/model/product-form.schema";
import type { CompanyProductDto, UpsertProductRequest, WorkspaceCategoryDto } from "@/features/workspace/model/workspace.types";
import styles from "@/features/workspace/screens/WorkspaceScreen.module.css";

interface ProductFormProps {
  categories: WorkspaceCategoryDto[];
  initialProduct?: CompanyProductDto | null;
  submitLabel: string;
  busy: boolean;
  onSubmit: (payload: UpsertProductRequest) => Promise<void>;
}

const toPrettyJson = (value: unknown, fallback: string): string => {
  if (typeof value === "undefined" || value === null) {
    return fallback;
  }

  try {
    return JSON.stringify(value, null, 2);
  } catch {
    return fallback;
  }
};

const toRequestPayload = (values: ProductFormValues): UpsertProductRequest => {
  let detail: unknown;
  let images: unknown;

  try {
    detail = values.detailJson ? JSON.parse(values.detailJson) : {};
  } catch {
    detail = {};
  }

  try {
    images = values.imagesJson ? JSON.parse(values.imagesJson) : [];
  } catch {
    images = [];
  }

  return {
    name: values.name,
    slug: values.slug,
    description: values.description,
    price: Number(values.price),
    oldPrice: values.oldPrice && values.oldPrice.length > 0 ? Number(values.oldPrice) : null,
    minStock: Number(values.minStock),
    categoryId: Number(values.categoryId),
    hasVariants: values.hasVariants,
    detail,
    images,
  };
};

export function ProductForm({ categories, initialProduct, submitLabel, busy, onSubmit }: ProductFormProps) {
  const form = useForm<z.input<typeof productFormSchema>, unknown, ProductFormValues>({
    resolver: zodResolver(productFormSchema),
    defaultValues: {
      name: initialProduct?.name ?? "",
      slug: initialProduct?.slug ?? "",
      description: initialProduct?.description ?? "",
      price: initialProduct?.price ?? 0,
      oldPrice: initialProduct?.oldPrice != null ? String(initialProduct.oldPrice) : "",
      minStock: initialProduct?.minStock ?? 0,
      categoryId: initialProduct?.categoryId ?? 0,
      hasVariants: initialProduct?.hasVariants ?? false,
      detailJson: toPrettyJson(initialProduct?.detail, "{}"),
      imagesJson: toPrettyJson(initialProduct?.images, "[]"),
    },
  });

  return (
    <form
      className={styles.formGrid}
      onSubmit={form.handleSubmit(async (values) => {
        await onSubmit(toRequestPayload(values));
      })}
    >
      <label className={styles.field}>
        Name
        <input className={styles.input} {...form.register("name")} />
        {form.formState.errors.name ? <span className={styles.error}>{form.formState.errors.name.message}</span> : null}
      </label>

      <label className={styles.field}>
        Slug
        <input className={styles.input} {...form.register("slug")} />
        {form.formState.errors.slug ? <span className={styles.error}>{form.formState.errors.slug.message}</span> : null}
      </label>

      <label className={styles.field}>
        Description
        <textarea className={styles.textarea} rows={3} {...form.register("description")} />
        {form.formState.errors.description ? (
          <span className={styles.error}>{form.formState.errors.description.message}</span>
        ) : null}
      </label>

      <label className={styles.field}>
        Price
        <input
          type="number"
          step="0.01"
          className={styles.input}
          {...form.register("price", { valueAsNumber: true })}
        />
        {form.formState.errors.price ? <span className={styles.error}>{form.formState.errors.price.message}</span> : null}
      </label>

      <label className={styles.field}>
        Old price
        <input type="number" step="0.01" className={styles.input} {...form.register("oldPrice")} />
        {form.formState.errors.oldPrice ? (
          <span className={styles.error}>{form.formState.errors.oldPrice.message as string}</span>
        ) : null}
      </label>

      <label className={styles.field}>
        Min stock
        <input type="number" className={styles.input} {...form.register("minStock", { valueAsNumber: true })} />
        {form.formState.errors.minStock ? (
          <span className={styles.error}>{form.formState.errors.minStock.message}</span>
        ) : null}
      </label>

      <label className={styles.field}>
        Category
        <select className={styles.select} {...form.register("categoryId", { valueAsNumber: true })}>
          <option value={0}>Select category</option>
          {categories.map((category) => (
            <option key={category.id} value={category.id}>
              {category.name}
            </option>
          ))}
        </select>
        {form.formState.errors.categoryId ? (
          <span className={styles.error}>{form.formState.errors.categoryId.message}</span>
        ) : null}
      </label>

      <label className={styles.inlineField}>
        <input type="checkbox" {...form.register("hasVariants")} />
        Has variants
      </label>

      <label className={styles.field}>
        Detail (JSON)
        <textarea className={styles.textarea} rows={4} {...form.register("detailJson")} />
      </label>

      <label className={styles.field}>
        Images (JSON)
        <textarea className={styles.textarea} rows={4} {...form.register("imagesJson")} />
      </label>

      <button type="submit" className={styles.primaryButton} disabled={busy || form.formState.isSubmitting}>
        {busy || form.formState.isSubmitting ? "Saving..." : submitLabel}
      </button>
    </form>
  );
}

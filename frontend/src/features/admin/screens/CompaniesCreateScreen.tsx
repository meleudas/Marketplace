"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { type BaseRecord, type HttpError, useCreate } from "@refinedev/core";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "@refinedev/react-hook-form";
import { CompanyForm } from "@/features/admin/screens/CompanyForm";
import { buildCompanyPayload } from "@/features/admin/screens/companies.form";
import {
  companyFormSchema,
  defaultCompanyFormValues,
  type CompanyFormValues,
} from "@/features/admin/validation/company-form.schema";
import {
  applyServerFieldErrors,
  parseAdminFormError,
} from "@/features/admin/validation/admin-form-errors";
import styles from "@/features/admin/screens/AdminScreens.module.css";

export function CompaniesCreateScreen() {
  const router = useRouter();
  const { mutateAsync, mutation } = useCreate();
  const isPending = mutation.isPending;
  const [error, setError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    setError: setFormError,
    formState: { errors, isSubmitting },
  } = useForm<BaseRecord, HttpError, CompanyFormValues>({
    resolver: zodResolver(companyFormSchema),
    defaultValues: defaultCompanyFormValues,
  });

  const onSubmit = async (values: CompanyFormValues) => {
    setError(null);

    try {
      await mutateAsync({
        resource: "companies",
        values: buildCompanyPayload(values),
      });

      router.push("/admin/companies");
    } catch (requestError) {
      const parsed = parseAdminFormError(requestError, "Failed to create company.");
      applyServerFieldErrors(setFormError, parsed.fieldErrors);
      setError(parsed.message);
    }
  };

  return (
    <section className={styles.panel}>
      <h2 className={styles.title}>Create company</h2>
      <p className={styles.subtitle}>Create a company using admin API.</p>

      <form onSubmit={handleSubmit(onSubmit)}>
        <CompanyForm register={register} errors={errors} disabled={isPending || isSubmitting} />

        <div className={styles.formActions}>
          <button type="submit" className={styles.button} disabled={isPending || isSubmitting}>
            {isPending || isSubmitting ? "Creating..." : "Create"}
          </button>
          <button
            type="button"
            className={styles.ghostButton}
            disabled={isPending || isSubmitting}
            onClick={() => router.push("/admin/companies")}
          >
            Cancel
          </button>
        </div>
      </form>

      {error ? <p className={styles.error}>{error}</p> : null}
    </section>
  );
}


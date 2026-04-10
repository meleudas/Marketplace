"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { type BaseRecord, type HttpError, useOne, useUpdate } from "@refinedev/core";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "@refinedev/react-hook-form";
import { CompanyForm } from "@/features/admin/screens/CompanyForm";
import {
  buildCompanyPayload,
  companyDtoToFormValues,
} from "@/features/admin/screens/companies.form";
import type { CompanyDto } from "@/features/admin/model/admin.types";
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

interface CompaniesEditScreenProps {
  id: string;
}

export function CompaniesEditScreen({ id }: CompaniesEditScreenProps) {
  const router = useRouter();
  const { mutateAsync, mutation } = useUpdate();
  const isPending = mutation.isPending;
  const { query } = useOne<CompanyDto>({
    resource: "companies",
    id,
  });

  const [error, setError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    reset,
    setError: setFormError,
    formState: { errors, isSubmitting },
  } = useForm<BaseRecord, HttpError, CompanyFormValues>({
    resolver: zodResolver(companyFormSchema),
    defaultValues: defaultCompanyFormValues,
  });

  useEffect(() => {
    if (!query.data?.data) {
      return;
    }

    reset(companyDtoToFormValues(query.data.data));
  }, [query.data?.data, reset]);

  const onSubmit = async (values: CompanyFormValues) => {
    setError(null);

    try {
      await mutateAsync({
        resource: "companies",
        id,
        values: buildCompanyPayload(values),
      });

      router.push("/admin/companies");
    } catch (requestError) {
      const parsed = parseAdminFormError(requestError, "Failed to update company.");
      applyServerFieldErrors(setFormError, parsed.fieldErrors);
      setError(parsed.message);
    }
  };

  return (
    <section className={styles.panel}>
      <h2 className={styles.title}>Edit company</h2>
      <p className={styles.subtitle}>Update company details.</p>

      {query.isLoading ? <p className={styles.stateText}>Loading company...</p> : null}
      {query.isError ? <p className={styles.error}>Failed to load company.</p> : null}

      {!query.isLoading && !query.isError ? (
        <form onSubmit={handleSubmit(onSubmit)}>
          <CompanyForm register={register} errors={errors} disabled={isPending || isSubmitting} />

          <div className={styles.formActions}>
            <button type="submit" className={styles.button} disabled={isPending || isSubmitting}>
              {isPending || isSubmitting ? "Saving..." : "Save"}
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
      ) : null}

      {error ? <p className={styles.error}>{error}</p> : null}
    </section>
  );
}




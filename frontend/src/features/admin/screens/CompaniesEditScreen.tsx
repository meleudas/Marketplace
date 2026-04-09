"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useOne, useUpdate } from "@refinedev/core";
import { CompanyForm } from "@/features/admin/screens/CompanyForm";
import {
  buildCompanyPayload,
  companyDtoToFormState,
  defaultCompanyFormState,
  type CompanyFormState,
} from "@/features/admin/screens/companies.form";
import type { CompanyDto } from "@/features/admin/model/admin.types";
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

  const [editedForm, setEditedForm] = useState<CompanyFormState | null>(null);
  const [error, setError] = useState<string | null>(null);

  const form =
    editedForm ??
    (query.data?.data ? companyDtoToFormState(query.data.data) : defaultCompanyFormState);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);

    try {
      await mutateAsync({
        resource: "companies",
        id,
        values: buildCompanyPayload(form),
      });

      router.push("/admin/companies");
    } catch {
      setError("Failed to update company.");
    }
  };

  return (
    <section className={styles.panel}>
      <h2 className={styles.title}>Edit company</h2>
      <p className={styles.subtitle}>Update company details.</p>

      {query.isLoading ? <p className={styles.stateText}>Loading company...</p> : null}
      {query.isError ? <p className={styles.error}>Failed to load company.</p> : null}

      {!query.isLoading && !query.isError ? (
        <form onSubmit={handleSubmit}>
          <CompanyForm value={form} onChange={setEditedForm} disabled={isPending} />

          <div className={styles.formActions}>
            <button type="submit" className={styles.button} disabled={isPending}>
              {isPending ? "Saving..." : "Save"}
            </button>
            <button
              type="button"
              className={styles.ghostButton}
              disabled={isPending}
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




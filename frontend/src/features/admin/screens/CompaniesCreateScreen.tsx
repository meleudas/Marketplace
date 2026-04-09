"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useCreate } from "@refinedev/core";
import { CompanyForm } from "@/features/admin/screens/CompanyForm";
import {
  buildCompanyPayload,
  defaultCompanyFormState,
  type CompanyFormState,
} from "@/features/admin/screens/companies.form";
import styles from "@/features/admin/screens/AdminScreens.module.css";

export function CompaniesCreateScreen() {
  const router = useRouter();
  const { mutateAsync, mutation } = useCreate();
  const isPending = mutation.isPending;
  const [form, setForm] = useState<CompanyFormState>(defaultCompanyFormState);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);

    try {
      await mutateAsync({
        resource: "companies",
        values: buildCompanyPayload(form),
      });

      router.push("/admin/companies");
    } catch {
      setError("Failed to create company.");
    }
  };

  return (
    <section className={styles.panel}>
      <h2 className={styles.title}>Create company</h2>
      <p className={styles.subtitle}>Create a company using admin API.</p>

      <form onSubmit={handleSubmit}>
        <CompanyForm value={form} onChange={setForm} disabled={isPending} />

        <div className={styles.formActions}>
          <button type="submit" className={styles.button} disabled={isPending}>
            {isPending ? "Creating..." : "Create"}
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

      {error ? <p className={styles.error}>{error}</p> : null}
    </section>
  );
}


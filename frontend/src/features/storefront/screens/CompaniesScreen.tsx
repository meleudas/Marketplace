"use client";

import { useEffect, useState } from "react";
import { getCatalogCompanies } from "@/features/storefront/api/catalog.api";
import type { CatalogCompanyDto } from "@/features/storefront/model/catalog.types";
import { CompanyCard } from "@/features/storefront/ui/CompanyCard";
import { StateBlock } from "@/features/storefront/ui/StateBlock";
import { StorefrontLayout } from "@/features/storefront/ui/StorefrontLayout";
import styles from "./StorefrontScreen.module.css";

export function CompaniesScreen() {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [companies, setCompanies] = useState<CatalogCompanyDto[]>([]);

  useEffect(() => {
    const load = async () => {
      try {
        setLoading(true);
        setError(null);

        const data = await getCatalogCompanies();
        setCompanies(data);
      } catch {
        setError("Failed to load data");
      } finally {
        setLoading(false);
      }
    };

    void load();
  }, []);

  return (
    <StorefrontLayout title="Companies">
      {loading ? <StateBlock message="Loading..." /> : null}
      {error ? <StateBlock message={error} isError /> : null}

      {!loading && !error ? (
        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Company list</h2>
          {companies.length === 0 ? (
            <StateBlock message="No companies found" />
          ) : (
            <div className={styles.grid}>
              {companies.map((company) => (
                <CompanyCard key={company.id} company={company} />
              ))}
            </div>
          )}
        </section>
      ) : null}
    </StorefrontLayout>
  );
}


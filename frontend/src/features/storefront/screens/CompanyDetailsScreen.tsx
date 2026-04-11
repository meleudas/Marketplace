"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { getCatalogCompanyBySlug } from "@/features/storefront/api/catalog.api";
import type { CatalogCompanyDto } from "@/features/storefront/model/catalog.types";
import { StateBlock } from "@/features/storefront/ui/StateBlock";
import { StorefrontLayout } from "@/features/storefront/ui/StorefrontLayout";
import styles from "./StorefrontScreen.module.css";

interface CompanyDetailsScreenProps {
  slug: string;
}

export function CompanyDetailsScreen({ slug }: CompanyDetailsScreenProps) {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [company, setCompany] = useState<CatalogCompanyDto | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        setLoading(true);
        setError(null);

        const data = await getCatalogCompanyBySlug(slug);
        setCompany(data);
      } catch {
        setError("Failed to load data");
      } finally {
        setLoading(false);
      }
    };

    void load();
  }, [slug]);

  return (
    <StorefrontLayout title="Company details">
      <Link href="/companies" className={styles.actionLink}>
        Back to companies
      </Link>

      {loading ? <StateBlock message="Loading..." /> : null}
      {error ? <StateBlock message={error} isError /> : null}
      {!loading && !error && !company ? <StateBlock message="No companies found" /> : null}

      {!loading && !error && company ? (
        <article className={styles.detailCard}>
          <h2 className={styles.sectionTitle}>{company.name}</h2>
          <p className={styles.text}>Slug: {company.slug}</p>

          {/* eslint-disable-next-line @next/next/no-img-element */}
          {company.imageUrl ? <img src={company.imageUrl} alt={company.name} className={styles.detailImage} /> : null}

          {company.description ? <p className={styles.text}>{company.description}</p> : null}
          <p className={styles.text}>Email: {company.contactEmail}</p>
          <p className={styles.text}>Phone: {company.contactPhone}</p>
          <p className={styles.text}>
            Address: {company.address.street}, {company.address.city}, {company.address.state}, {company.address.postalCode},{" "}
            {company.address.country}
          </p>
          <p className={styles.text}>Rating: {company.rating ?? "-"}</p>
          <p className={styles.text}>Reviews: {company.reviewCount}</p>
          <p className={styles.text}>Followers: {company.followerCount}</p>
        </article>
      ) : null}
    </StorefrontLayout>
  );
}

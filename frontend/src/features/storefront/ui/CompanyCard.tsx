import Link from "next/link";
import type { CatalogCompanyDto } from "@/features/storefront/model/catalog.types";
import styles from "./CompanyCard.module.css";

interface CompanyCardProps {
  company: CatalogCompanyDto;
}

export function CompanyCard({ company }: CompanyCardProps) {
  return (
    <Link href={`/companies/${company.slug}`} className={styles.cardLink}>
      <article className={styles.card}>
        {/* eslint-disable-next-line @next/next/no-img-element */}
        {company.imageUrl ? <img src={company.imageUrl} alt={company.name} className={styles.image} /> : null}

        <div className={styles.body}>
          <h3 className={styles.title}>{company.name}</h3>
          <p className={styles.text}>{company.description}</p>

          <p className={styles.meta}>Slug: {company.slug}</p>
          <p className={styles.meta}>Email: {company.contactEmail}</p>
          <p className={styles.meta}>Phone: {company.contactPhone}</p>
          <p className={styles.meta}>
            Address: {company.address.street}, {company.address.city}, {company.address.state},{" "}
            {company.address.postalCode}, {company.address.country}
          </p>

          <p className={styles.meta}>Rating: {company.rating ?? "-"}</p>
          <p className={styles.meta}>Reviews: {company.reviewCount}</p>
        </div>
      </article>
    </Link>
  );
}


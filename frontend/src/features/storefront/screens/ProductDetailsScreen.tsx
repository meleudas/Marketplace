"use client";

import Image from "next/image";
import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import {
  getCatalogProductBySlug,
  getProductAvailability,
} from "@/features/storefront/api/catalog.api";
import type {
  CatalogProductDetailDto,
  ProductAvailabilityDto,
} from "@/features/storefront/model/catalog.types";
import { StateBlock } from "@/features/storefront/ui/StateBlock";
import { StorefrontLayout } from "@/features/storefront/ui/StorefrontLayout";
import styles from "./StorefrontScreen.module.css";

interface ProductDetailsScreenProps {
  slug: string;
}

const formatPrice = (value: number | null | undefined): string => {
  if (typeof value !== "number") {
    return "-";
  }

  return `$${value.toFixed(2)}`;
};

export function ProductDetailsScreen({ slug }: ProductDetailsScreenProps) {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [productDetails, setProductDetails] = useState<CatalogProductDetailDto | null>(null);
  const [availability, setAvailability] = useState<ProductAvailabilityDto | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        setLoading(true);
        setError(null);

        const details = await getCatalogProductBySlug(slug);
        setProductDetails(details);

        const hasAvailabilityInDetails = typeof details.product.availableQty === "number";

        if (!hasAvailabilityInDetails) {
          const extraAvailability = await getProductAvailability(details.product.companyId, String(details.product.id));
          setAvailability(extraAvailability);
        }
      } catch {
        setError("Failed to load data");
      } finally {
        setLoading(false);
      }
    };

    void load();
  }, [slug]);

  const resolvedAvailability = useMemo(() => {
    if (!productDetails) {
      return null;
    }

    const availableQty = productDetails.product.availableQty ?? availability?.availableQty;
    const availabilityStatus =
      productDetails.product.availabilityStatus ?? availability?.availabilityStatus;

    if (typeof availableQty !== "number" || !availabilityStatus) {
      return null;
    }

    return { availableQty, availabilityStatus };
  }, [availability, productDetails]);

  const previewImage = productDetails?.images[0]?.imageUrl ?? null;

  return (
    <StorefrontLayout title="Product details">
      <Link href="/products" className={styles.actionLink}>
        Back to products
      </Link>

      {loading ? <StateBlock message="Loading..." /> : null}
      {error ? <StateBlock message={error} isError /> : null}

      {!loading && !error && !productDetails ? <StateBlock message="No products found" /> : null}

      {!loading && !error && productDetails ? (
        <article className={styles.detailCard}>
          <h2 className={styles.sectionTitle}>{productDetails.product.name}</h2>
          <p className={styles.text}>Slug: {productDetails.product.slug}</p>

          {previewImage ? (
            <Image
              src={previewImage}
              alt={productDetails.product.name}
              className={styles.detailImage}
              width={960}
              height={540}
            />
          ) : null}

          <p className={styles.text}>Price: {formatPrice(productDetails.product.price)}</p>
          {typeof productDetails.product.oldPrice === "number" ? (
            <p className={styles.text}>Old price: {formatPrice(productDetails.product.oldPrice)}</p>
          ) : null}

          {productDetails.product.description ? (
            <p className={styles.text}>{productDetails.product.description}</p>
          ) : null}

          {resolvedAvailability ? (
            <>
              <p className={styles.text}>Available: {resolvedAvailability.availableQty}</p>
              <p className={styles.text}>Status: {resolvedAvailability.availabilityStatus}</p>
            </>
          ) : null}
        </article>
      ) : null}
    </StorefrontLayout>
  );
}


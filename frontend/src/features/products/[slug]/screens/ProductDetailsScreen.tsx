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
import { useCartStore } from "@/features/cart/model/cart.store";
import { StateBlock } from "@/features/storefront/ui/StateBlock";
import { StorefrontLayout } from "@/features/storefront/ui/StorefrontLayout";
import styles from "@/features/storefront/ui/StorefrontScreen.module.css";
import { apiClient } from "@/shared/api/http.client";

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

  const [addingToCart, setAddingToCart] = useState(false);
  const [cartSuccess, setCartSuccess] = useState(false);
  const [cartError, setCartError] = useState<string | null>(null);

  const { loadCart } = useCartStore();

  const handleAddToCart = async () => {
    if (!productDetails) return;
    setAddingToCart(true);
    setCartError(null);
    try {
      await apiClient.post("/me/cart/items", {
        productId: productDetails.product.id,
        quantity: 1,
      });
      setCartSuccess(true);
      loadCart();
    } catch (err: any) {
      console.error(err);
      if (err.response?.status === 401) {
        setCartError("Будь ласка, увійдіть у свій акаунт, щоб додати книгу до кошика.");
      } else {
        setCartError("Помилка при додаванні до кошика. Спробуйте ще раз.");
      }
    } finally {
      setAddingToCart(false);
    }
  };

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

          {/* Cart Action Block */}
          <div style={{ marginTop: "24px", borderTop: "1px solid #2f2f2f", paddingTop: "20px" }}>
            {cartSuccess ? (
              <div style={{ display: "flex", flexDirection: "column", gap: "12px" }}>
                <p style={{ color: "#22c55e", fontWeight: "600", fontSize: "16px" }}>✓ Книгу додано в кошик!</p>
                <div style={{ display: "flex", gap: "12px" }}>
                  <Link href="/checkout" style={{
                    display: "inline-flex",
                    alignItems: "center",
                    justifyContent: "center",
                    height: "44px",
                    padding: "0 24px",
                    borderRadius: "8px",
                    background: "#ee0290",
                    color: "#ffffff",
                    fontSize: "15px",
                    fontWeight: "600",
                    textDecoration: "none",
                    transition: "background 0.15s"
                  }}>
                    Оформити замовлення
                  </Link>
                  <button
                    onClick={() => setCartSuccess(false)}
                    style={{
                      background: "transparent",
                      border: "1px solid #3e3e3e",
                      borderRadius: "8px",
                      color: "#ffffff",
                      padding: "0 20px",
                      fontSize: "15px",
                      fontWeight: "600",
                      cursor: "pointer"
                    }}
                  >
                    Продовжити покупки
                  </button>
                </div>
              </div>
            ) : (
              <div style={{ display: "flex", flexDirection: "column", gap: "12px" }}>
                <button
                  onClick={handleAddToCart}
                  disabled={addingToCart || resolvedAvailability?.availableQty === 0}
                  style={{
                    display: "inline-flex",
                    alignItems: "center",
                    justifyContent: "center",
                    height: "48px",
                    padding: "0 32px",
                    borderRadius: "8px",
                    background: resolvedAvailability?.availableQty === 0 ? "#3e3e3e" : "#ee0290",
                    color: "#ffffff",
                    fontSize: "16px",
                    fontWeight: "700",
                    border: "none",
                    cursor: resolvedAvailability?.availableQty === 0 ? "not-allowed" : "pointer",
                    transition: "background 0.15s",
                    width: "fit-content"
                  }}
                >
                  {addingToCart ? "Додавання..." : resolvedAvailability?.availableQty === 0 ? "Немає в наявності" : "Додати в кошик"}
                </button>
                {cartError && (
                  <p style={{ color: "#ef4444", fontSize: "14px", fontWeight: "500" }}>{cartError}</p>
                )}
              </div>
            )}
          </div>
        </article>
      ) : null}
    </StorefrontLayout>
  );
}


"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import type { AddedToCartProduct } from "@/features/cart/model/added-to-cart.types";
import { Button } from "@/shared/ui/Button";
import { CloseIcon } from "@/shared/ui/icons";
import iconStyles from "@/shared/ui/icons/Icon.module.css";
import styles from "./AddToCartDialog.module.css";

interface AddToCartDialogProps {
  open: boolean;
  product: AddedToCartProduct | null;
  onClose: () => void;
}

const formatPrice = (value: number): string =>
  `${new Intl.NumberFormat("uk-UA", { maximumFractionDigits: 0 }).format(value)} грн`;

export function AddToCartDialog({ open, product, onClose }: AddToCartDialogProps) {
  const router = useRouter();

  useEffect(() => {
    if (!open) {
      return;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onClose();
      }
    };

    const previousBodyOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    window.addEventListener("keydown", handleKeyDown);

    return () => {
      document.body.style.overflow = previousBodyOverflow;
      window.removeEventListener("keydown", handleKeyDown);
    };
  }, [onClose, open]);

  if (!open || !product) {
    return null;
  }

  return (
    <div className={styles.overlay} onClick={onClose}>
      <div
        className={styles.dialog}
        role="dialog"
        aria-modal="true"
        aria-labelledby="add-to-cart-dialog-title"
        onClick={(event) => event.stopPropagation()}
      >
        <button
          type="button"
          className={styles.closeButton}
          aria-label="Закрити"
          onClick={onClose}
        >
          <CloseIcon className={iconStyles.icon} />
        </button>

        <div className={styles.successBadge} aria-hidden="true">
          <span className={styles.checkMark}>✓</span>
        </div>

        <h2 id="add-to-cart-dialog-title" className={styles.title}>
          Додано до кошика
        </h2>
        <p className={styles.subtitle}>Ваша книга вже чекає на оформлення замовлення.</p>

        <div className={styles.productPreview}>
          <div className={styles.productMedia}>
            {product.imageUrl ? (
              // eslint-disable-next-line @next/next/no-img-element
              <img
                src={product.imageUrl}
                alt=""
                className={styles.productImage}
              />
            ) : (
              <div className={styles.productPlaceholder} aria-hidden="true" />
            )}
          </div>

          <div className={styles.productMeta}>
            <p className={styles.productTitle} data-testid="add-to-cart-dialog-product-title">
              {product.title}
            </p>
            {typeof product.price === "number" ? (
              <p className={styles.productPrice} data-testid="add-to-cart-dialog-product-price">
                {formatPrice(product.price)}
              </p>
            ) : null}
          </div>
        </div>

        <div className={styles.actions}>
          <Button
            variant="primary"
            size="lg"
            fullWidth
            onClick={() => {
              onClose();
              router.push("/cart");
            }}
          >
            Перейти до кошика
          </Button>
          <Button variant="secondary" size="lg" fullWidth onClick={onClose}>
            Продовжити покупки
          </Button>
        </div>
      </div>
    </div>
  );
}

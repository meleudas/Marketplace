"use client";

import { useEffect, useState, useMemo, useCallback } from "react";
import { useRouter } from "next/navigation";
import Image from "next/image";
import Link from "next/link";
import { useAuth } from "@/features/auth/model/auth.store";
import { useCartStore } from "@/features/cart/model/cart.store";
import { getCatalogProducts } from "@/features/storefront/api/catalog.api";
import {
  fetchMyCart,
  updateCartItemQuantity,
  removeCartItem,
  type CartDto,
} from "@/features/checkout/api/checkout.api";
import type { CatalogProductListItemDto } from "@/features/storefront/model/catalog.types";
import { formatCartPrice } from "@/features/cart/lib/format-cart-price";
import { PageLayout, Spinner } from "@/shared/ui";
import styles from "./CartScreen.module.css";

interface CartItemWithMeta {
  id: number;
  productId: number;
  quantity: number;
  priceAtMoment: number;
  lineTotal: number;
  name: string;
  imageUrl: string;
  slug: string;
  author: string;
  stockStatus: string;
}

export function CartScreen() {
  const { isAuthenticated, initialized, loadMe } = useAuth();
  const router = useRouter();

  useEffect(() => {
    loadMe();
  }, [loadMe]);

  const [cart, setCart] = useState<CartDto | null>(null);
  const [catalogProducts, setCatalogProducts] = useState<
    CatalogProductListItemDto[]
  >([]);
  const [loading, setLoading] = useState(true);

  const { setTotalItems } = useCartStore();

  useEffect(() => {
    if (!initialized) return;
    if (!isAuthenticated) {
      setLoading(false);
      return;
    }

    const loadData = async () => {
      try {
        const [cartData, products] = await Promise.all([
          fetchMyCart(),
          getCatalogProducts(),
        ]);
        setCart(cartData);
        setCatalogProducts(products);
        setTotalItems(cartData.totalItems);
      } catch {
        // Cart may not exist yet
        setCart(null);
        setTotalItems(0);
      } finally {
        setLoading(false);
      }
    };

    loadData();
  }, [initialized, isAuthenticated, setTotalItems]);

  useEffect(() => {
    if (initialized && !isAuthenticated) {
      router.push("/auth/login?redirect=/cart");
    }
  }, [initialized, isAuthenticated, router]);

  const cartItemsWithMetadata: CartItemWithMeta[] = useMemo(() => {
    if (!cart?.items) return [];
    return cart.items.map((item) => {
      const product = catalogProducts.find((p) => p.id === item.productId) as
        | (CatalogProductListItemDto & { author?: string })
        | undefined;
      return {
        id: item.id,
        productId: item.productId,
        quantity: item.quantity,
        priceAtMoment: item.priceAtMoment,
        lineTotal: item.lineTotal,
        name: product?.name || `Товар #${item.productId}`,
        imageUrl: product?.imageUrls?.[0] || "",
        slug: product?.slug || "",
        author: product?.author || "Невідомий автор",
        stockStatus:
          product?.availabilityStatus === "InStock" || (product?.stock ?? 0) > 0
            ? "В наявності"
            : "Немає в наявності",
      };
    });
  }, [cart, catalogProducts]);

  const handleUpdateQty = useCallback(
    async (itemId: number, currentQty: number, delta: number) => {
      const newQty = currentQty + delta;
      if (newQty < 1) return;
      try {
        const updatedCart = await updateCartItemQuantity(itemId, newQty);
        setCart(updatedCart);
        setTotalItems(updatedCart.totalItems);
      } catch {
      }
    },
    [setTotalItems],
  );

  const handleDeleteItem = useCallback(async (itemId: number) => {
    try {
      const updatedCart = await removeCartItem(itemId);
      setCart(updatedCart);
      setTotalItems(updatedCart.totalItems);
    } catch {
    }
  }, [setTotalItems]);

  const totalAmount = cart?.totalAmount ?? 0;

  if (loading) {
    return (
      <PageLayout headerProps={{ cartHref: "/cart" }}>
        <div className={styles.root}>
          <div className={styles.loadingState}>
            <Spinner />
            <span className={styles.loadingText}>
              Завантаження кошика...
            </span>
          </div>
        </div>
      </PageLayout>
    );
  }

  if (!cart || cartItemsWithMetadata.length === 0) {
    return (
      <PageLayout headerProps={{ cartHref: "/cart" }}>
        <div className={styles.root}>
          <div className={styles.titleRow}>
            <h1 className={styles.pageTitle}>Кошик</h1>
            <Link href="/" className={styles.closeBtn} aria-label="Закрити">
              <svg width="32" height="32" viewBox="0 0 32 32" fill="none">
                <path
                  d="M8 8L24 24M24 8L8 24"
                  stroke="#ffffff"
                  strokeWidth="2"
                  strokeLinecap="round"
                />
              </svg>
            </Link>
          </div>
          <div className={styles.emptyState}>
            <svg
              className={styles.emptyIcon}
              width="64"
              height="64"
              viewBox="0 0 24 24"
              fill="none"
            >
              <path
                d="M6 2L3 6v14a2 2 0 002 2h14a2 2 0 002-2V6l-3-4zM3 6h18M16 10a4 4 0 01-8 0"
                stroke="currentColor"
                strokeWidth="1.5"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </svg>
            <h2 className={styles.emptyTitle}>Кошик порожній</h2>
            <p className={styles.emptyText}>
              Додайте товари до кошика, щоб продовжити покупки
            </p>
            <Link href="/" className={styles.emptyBtn}>
              Перейти до каталогу
            </Link>
          </div>
        </div>
      </PageLayout>
    );
  }

  return (
    <PageLayout headerProps={{ cartHref: "/cart" }}>
      <div className={styles.root}>
        <div className={styles.cartContent}>
          <div className={styles.titleRow}>
            <h1 className={styles.pageTitle}>Кошик</h1>
            <Link href="/" className={styles.closeBtn} aria-label="Закрити">
              <svg width="32" height="32" viewBox="0 0 32 32" fill="none">
                <path
                  d="M8 8L24 24M24 8L8 24"
                  stroke="#ffffff"
                  strokeWidth="2"
                  strokeLinecap="round"
                />
              </svg>
            </Link>
          </div>

          <div className={styles.colsContainer}>
            <div className={styles.leftCol}>
              <div className={styles.itemsList}>
                {cartItemsWithMetadata.map((item) => (
                  <div key={item.id} className={styles.cartItem}>
                    <div className={styles.itemTopRow}>
                      <div className={styles.itemImageWrap}>
                        {item.imageUrl ? (
                          <Image
                            src={item.imageUrl}
                            alt={item.name}
                            fill
                            className={styles.itemImage}
                            sizes="74px"
                          />
                        ) : (
                          <div className={styles.itemImageFallback} />
                        )}
                      </div>
                      <div className={styles.itemMeta}>
                        <span className={styles.authorText}>{item.author}</span>
                        <div className={styles.stockInfo}>
                          <span className={styles.stockGreen}>
                            {item.stockStatus}
                          </span>
                          <span className={styles.bookTitle}>{item.name}</span>
                        </div>
                      </div>
                      <button
                        type="button"
                        className={styles.trashBtn}
                        onClick={() => handleDeleteItem(item.id)}
                        aria-label="Видалити"
                      >
                        <svg
                          width="24"
                          height="24"
                          viewBox="0 0 24 24"
                          fill="none"
                        >
                          <path
                            d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                            stroke="currentColor"
                            strokeWidth="1.5"
                            strokeLinecap="round"
                            strokeLinejoin="round"
                          />
                        </svg>
                      </button>
                    </div>

                    <div className={styles.itemBottomRow}>
                      <div className={styles.qtyEditor}>
                        <button
                          type="button"
                          className={styles.qtyBtn}
                          onClick={() =>
                            handleUpdateQty(item.id, item.quantity, -1)
                          }
                          disabled={item.quantity <= 1}
                          aria-label="Зменшити кількість"
                        >
                          <svg
                            width="16"
                            height="16"
                            viewBox="0 0 16 16"
                            fill="none"
                          >
                            <path
                              d="M3 8h10"
                              stroke="#ffffff"
                              strokeWidth="2"
                              strokeLinecap="round"
                            />
                          </svg>
                        </button>
                        <span className={styles.qtyText}>{item.quantity}</span>
                        <button
                          type="button"
                          className={styles.qtyBtn}
                          onClick={() =>
                            handleUpdateQty(item.id, item.quantity, 1)
                          }
                          aria-label="Збільшити кількість"
                        >
                          <svg
                            width="16"
                            height="16"
                            viewBox="0 0 16 16"
                            fill="none"
                          >
                            <path
                              d="M8 3v10M3 8h10"
                              stroke="#ffffff"
                              strokeWidth="2"
                              strokeLinecap="round"
                            />
                          </svg>
                        </button>
                      </div>
                      <span className={styles.priceText}>
                        {formatCartPrice(item.lineTotal)} грн.
                      </span>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div className={styles.rightCol}>
              <div className={styles.bottomBar}>
                <div className={styles.totalsRow}>
                  <span className={styles.totalsLabel}>Разом</span>
                  <span className={styles.totalsAmount}>{formatCartPrice(totalAmount)}грн</span>
                </div>
                <Link href="/checkout" className={styles.ctaBtn}>
                  Оформити замовлення
                </Link>
              </div>
            </div>
          </div>
        </div>
      </div>
    </PageLayout>
  );
}

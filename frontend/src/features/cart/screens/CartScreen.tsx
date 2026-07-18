"use client";

import { useEffect, useState, useMemo, useCallback } from "react";
import Image from "next/image";
import Link from "next/link";
import { useAuth } from "@/features/auth/model/auth.store";
import {
  getGuestCart,
  removeGuestCartItem,
  setGuestCartItemQuantity,
  subscribeToGuestCart,
} from "@/features/cart/lib/guest-cart.storage";
import { useCartStore } from "@/features/cart/model/cart.store";
import { getCatalogProductBySlug, getCatalogProducts } from "@/features/storefront/api/catalog.api";
import {
  fetchMyCart,
  updateCartItemQuantity,
  removeCartItem,
  type CartDto,
} from "@/features/checkout/api/checkout.api";
import type { CatalogProductListItemDto } from "@/features/storefront/model/catalog.types";
import { formatCartPrice } from "@/features/cart/lib/format-cart-price";
import { getProductAuthor } from "@/features/products/[slug]/lib/product-details.lib";
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

  useEffect(() => {
    loadMe();
  }, [loadMe]);

  const [cart, setCart] = useState<CartDto | null>(null);
  const [guestQuantities, setGuestQuantities] = useState<Record<number, number>>({});
  const [catalogProducts, setCatalogProducts] = useState<
    CatalogProductListItemDto[]
  >([]);
  const [loading, setLoading] = useState(true);
  const [enrichedAuthors, setEnrichedAuthors] = useState<Record<number, string>>({});
  const [enrichedStock, setEnrichedStock] = useState<Record<number, boolean>>({});

  const { setTotalItems } = useCartStore();

  useEffect(() => {
    if (!initialized) return;

    if (!isAuthenticated) {
      const syncGuestQuantities = () => {
        const items = getGuestCart();
        setGuestQuantities(
          items.reduce<Record<number, number>>((acc, item) => {
            acc[item.productId] = item.quantity;
            return acc;
          }, {}),
        );
      };

      syncGuestQuantities();
      const unsubscribe = subscribeToGuestCart(syncGuestQuantities);

      const loadProducts = async () => {
        try {
          const products = await getCatalogProducts();
          setCatalogProducts(products);
        } catch {
          setCatalogProducts([]);
        } finally {
          setLoading(false);
        }
      };

      void loadProducts();
      return unsubscribe;
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

    void loadData();
  }, [initialized, isAuthenticated, setTotalItems]);

  // Enrich cart items with product detail data (author, stock) when catalog list lacks it
  useEffect(() => {
    if (loading || catalogProducts.length === 0) return;

    const productIdsToEnrich: { productId: number; slug: string }[] = [];

    if (isAuthenticated && cart?.items) {
      for (const item of cart.items) {
        const product = catalogProducts.find((p) => p.id === item.productId);
        if (product && !product.author) {
          productIdsToEnrich.push({ productId: item.productId, slug: product.slug });
        }
      }
    } else {
      const guestProductIds = Object.keys(guestQuantities).map(Number);
      for (const productId of guestProductIds) {
        const product = catalogProducts.find((p) => p.id === productId);
        if (product && !product.author) {
          productIdsToEnrich.push({ productId, slug: product.slug });
        }
      }
    }

    if (productIdsToEnrich.length === 0) return;

    let cancelled = false;

    const enrich = async () => {
      const authors: Record<number, string> = {};
      const stock: Record<number, boolean> = {};

      await Promise.allSettled(
        productIdsToEnrich.map(async ({ productId, slug }) => {
          try {
            const detail = await getCatalogProductBySlug(slug);
            if (!cancelled) {
              const author = detail.detail ? getProductAuthor(detail.detail) : null;
              if (author) {
                authors[productId] = author;
              }
              stock[productId] = detail.product.availabilityStatus === "InStock" || (detail.product.stock ?? 0) > 0;
            }
          } catch {
            // Silently skip if enrichment fails
          }
        }),
      );

      if (!cancelled) {
        setEnrichedAuthors((prev) => ({ ...prev, ...authors }));
        setEnrichedStock((prev) => ({ ...prev, ...stock }));
      }
    };

    void enrich();

    return () => {
      cancelled = true;
    };
  }, [loading, catalogProducts, cart, guestQuantities, isAuthenticated]);

  const cartItemsWithMetadata: CartItemWithMeta[] = useMemo(() => {
    if (!initialized) return [];

    if (isAuthenticated) {
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
          author: product?.author || enrichedAuthors[item.productId] || "Невідомий автор",
          stockStatus:
            (enrichedStock[item.productId] ?? (product?.availabilityStatus === "InStock" || (product?.stock ?? 0) > 0))
              ? "В наявності"
              : "Немає в наявності",
        };
      });
    }

    return Object.entries(guestQuantities)
      .map(([productIdText, quantity]) => {
        const productId = Number(productIdText);
        const product = catalogProducts.find((p) => p.id === productId) as
          | (CatalogProductListItemDto & { author?: string })
          | undefined;

        if (!product) {
          return null;
        }

        const priceAtMoment = product.price;
        return {
          id: productId,
          productId,
          quantity,
          priceAtMoment,
          lineTotal: priceAtMoment * quantity,
          name: product.name,
          imageUrl: product.imageUrls?.[0] || "",
          slug: product.slug,
          author: product.author || enrichedAuthors[productId] || "Невідомий автор",
          stockStatus:
            (enrichedStock[productId] ?? (product.availabilityStatus === "InStock" || (product.stock ?? 0) > 0))
              ? "В наявності"
              : "Немає в наявності",
        } satisfies CartItemWithMeta;
      })
      .filter((item): item is CartItemWithMeta => item !== null);
  }, [cart, catalogProducts, guestQuantities, initialized, isAuthenticated]);

  const handleUpdateQty = useCallback(
    async (itemId: number, currentQty: number, delta: number) => {
      const newQty = currentQty + delta;
      if (newQty < 1) return;

      if (!isAuthenticated) {
        setGuestCartItemQuantity(itemId, newQty);
        return;
      }

      try {
        const updatedCart = await updateCartItemQuantity(itemId, newQty);
        setCart(updatedCart);
        setTotalItems(updatedCart.totalItems);
      } catch {
      }
    },
    [isAuthenticated, setTotalItems],
  );

  const handleDeleteItem = useCallback(
    async (itemId: number) => {
      if (!isAuthenticated) {
        removeGuestCartItem(itemId);
        return;
      }

      try {
        const updatedCart = await removeCartItem(itemId);
        setCart(updatedCart);
        setTotalItems(updatedCart.totalItems);
      } catch {
      }
    },
    [isAuthenticated, setTotalItems],
  );

  const totalAmount = isAuthenticated
    ? cart?.totalAmount ?? 0
    : cartItemsWithMetadata.reduce((sum, item) => sum + item.lineTotal, 0);

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

  if (cartItemsWithMetadata.length === 0) {
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

          {!isAuthenticated ? (
            <p className={styles.guestNotice}>
              Ви переглядаєте кошик як гість. Увійдіть або зареєструйтесь, щоб оформити замовлення.
            </p>
          ) : null}

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
                              stroke="currentColor"
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
                              stroke="currentColor"
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
                <Link
                  href={isAuthenticated ? "/checkout" : "/auth?redirect=/checkout"}
                  className={styles.ctaBtn}
                >
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

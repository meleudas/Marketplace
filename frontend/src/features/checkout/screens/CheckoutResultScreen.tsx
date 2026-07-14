"use client";

import { useEffect, useState, useRef } from "react";
import { useSearchParams } from "next/navigation";
import Link from "next/link";
import { PageLayout, Spinner } from "@/shared/ui";
import { apiClient } from "@/shared/api/http.client";
import type { OrderDetails, PagedOrdersResponse } from "@/features/me/api/me.api";
import styles from "./CheckoutResultScreen.module.css";

type ResultStatus = "checking" | "success" | "failed" | "pending" | "not_found" | "error";

export function CheckoutResultScreen() {
  const searchParams = useSearchParams();
  const orderNumber = searchParams.get("order_id") || "";

  const [status, setStatus] = useState<ResultStatus>(
    orderNumber ? "checking" : "error"
  );
  const [order, setOrder] = useState<OrderDetails | null>(null);
  const [errorMsg, setErrorMsg] = useState(
    orderNumber ? "" : "Номер замовлення не вказано в URL."
  );

  const shippingAddress = order?.addresses.find((a) => a.kind === "Shipping");
  const addressText = shippingAddress
    ? `${shippingAddress.city}, ${shippingAddress.street}`
    : "";

  const isUkrPoshta = addressText.toLowerCase().includes("укрпошта");
  const isNovaPoshta = addressText.toLowerCase().includes("нова пошта") || addressText.toLowerCase().includes("поштомат");
  const deliveryService = isNovaPoshta
    ? "Нова Пошта"
    : isUkrPoshta
      ? "Укрпошта"
      : "Самовивіз / Інша служба доставки";

  const pollIntervalRef = useRef<NodeJS.Timeout | null>(null);
  const retriesRef = useRef(0);
  const maxRetries = 6;

  const clearPoll = () => {
    if (pollIntervalRef.current) {
      clearInterval(pollIntervalRef.current);
      pollIntervalRef.current = null;
    }
  };

  useEffect(() => {
    if (!orderNumber) {
      return;
    }

    const checkStatus = async () => {
      try {
        const listResponse = await apiClient.get<PagedOrdersResponse>("/me/orders", {
          params: { search: orderNumber.trim() },
        });

        const matchedOrder = listResponse.data.items.find(
          (o) => o.orderNumber.toLowerCase() === orderNumber.toLowerCase()
        );

        if (!matchedOrder) {
          handleRetry();
          return;
        }

        const detailsResponse = await apiClient.get<OrderDetails>(
          `/me/orders/${matchedOrder.orderId}`
        );
        const orderData = detailsResponse.data;
        setOrder(orderData);

        const paymentStatus = orderData.payment?.status?.toLowerCase();

        if (paymentStatus === "completed" || paymentStatus === "captured" || paymentStatus === "success" || paymentStatus === "paid") {
          setStatus("success");
          clearPoll();
        } else if (paymentStatus === "failed" || paymentStatus === "failure") {
          setStatus("failed");
          clearPoll();
        } else {
          handleRetry(orderData);
        }
      } catch (err) {
        console.error("Error checking order status:", err);
        setErrorMsg("Помилка зв'язку з сервером при отриманні статусу платежу.");
        handleRetry();
      }
    };

    const handleRetry = (orderData?: OrderDetails) => {
      retriesRef.current += 1;
      if (retriesRef.current >= maxRetries) {
        if (orderData) {
          setStatus("pending");
        } else {
          setStatus("not_found");
        }
        clearPoll();
      }
    };

    void checkStatus();

    pollIntervalRef.current = setInterval(() => {
      void checkStatus();
    }, 2500);

    return () => clearPoll();
  }, [orderNumber]);

  const getStatusText = () => {
    switch (status) {
      case "success":
        return "Сплачено";
      case "failed":
        return "Помилка оплати";
      case "pending":
        return "Очікує підтвердження";
      default:
        return "Перевірка";
    }
  };

  const getStatusBadgeClass = () => {
    switch (status) {
      case "success":
        return styles.statusBadgeSuccess;
      case "failed":
        return styles.statusBadgeFailed;
      case "pending":
      default:
        return styles.statusBadgePending;
    }
  };

  // Render Checking/Loading State
  if (status === "checking") {
    return (
      <PageLayout>
        <div className={styles.root}>
          <div className={styles.card}>
            <div className={styles.spinnerWrapper}>
              <Spinner size="lg" />
              <p className={styles.spinnerText}>Перевіряємо статус оплати замовлення...</p>
            </div>
          </div>
        </div>
      </PageLayout>
    );
  }

  // Render Error / Missing ID State
  if (status === "error") {
    return (
      <PageLayout>
        <div className={styles.root}>
          <div className={styles.card}>
            <div className={`${styles.iconWrap} ${styles.failedIconWrap}`}>
              <svg width="36" height="36" viewBox="0 0 24 24" fill="none">
                <path d="M12 9v4m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
              </svg>
            </div>
            <h1 className={styles.title}>Помилка</h1>
            <p className={styles.subtitle}>{errorMsg}</p>
            <div className={styles.actions}>
              <Link href="/catalog" className={styles.btnPrimary}>На головну</Link>
            </div>
          </div>
        </div>
      </PageLayout>
    );
  }

  // Render Not Found State (if we polled but couldn't find the order on the backend)
  if (status === "not_found") {
    return (
      <PageLayout>
        <div className={styles.root}>
          <div className={styles.card}>
            <div className={`${styles.iconWrap} ${styles.failedIconWrap}`}>
              <svg width="36" height="36" viewBox="0 0 24 24" fill="none">
                <path d="M9.172 16.172a4 4 0 0 1 5.656 0M9 10h.01M15 10h.01M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0z" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
              </svg>
            </div>
            <h1 className={styles.title}>Замовлення не знайдено</h1>
            <p className={styles.subtitle}>Не вдалося підтвердити замовлення {orderNumber}. Перевірте ваш кабінет або зверніться до підтримки.</p>
            <div className={styles.actions}>
              <Link href="/me" className={styles.btnPrimary}>До кабінету</Link>
              <Link href="/catalog" className={styles.btnSecondary}>На головну</Link>
            </div>
          </div>
        </div>
      </PageLayout>
    );
  }

  return (
    <PageLayout>
      <div className={styles.root}>
        <div className={styles.card}>

          {/* Success State */}
          {status === "success" && (
            <>
              <div className={`${styles.iconWrap} ${styles.successIconWrap}`}>
                <svg width="40" height="40" viewBox="0 0 24 24" fill="none">
                  <path d="M20 6L9 17L4 12" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round" />
                </svg>
              </div>
              <h1 className={styles.title}>Оплачено успішно!</h1>
              <p className={styles.subtitle}>Дякуємо за покупку. Ваше замовлення прийнято в обробку.</p>
            </>
          )}

          {/* Failed State */}
          {status === "failed" && (
            <>
              <div className={`${styles.iconWrap} ${styles.failedIconWrap}`}>
                <svg width="36" height="36" viewBox="0 0 24 24" fill="none">
                  <path d="M18 6L6 18M6 6l12 12" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" />
                </svg>
              </div>
              <h1 className={styles.title}>Оплата не вдалася</h1>
              <p className={styles.subtitle}>Кошти не були списані. Будь ласка, спробуйте оплатити замовлення знову в кабінеті.</p>
            </>
          )}

          {status === "pending" && (
            <>
              <div className={`${styles.iconWrap} ${styles.pendingIconWrap}`}>
                <svg width="36" height="36" viewBox="0 0 24 24" fill="none">
                  <path d="M12 8v4l3 3m6-3a9 9 0 1 1-18 0 9 9 0 0 1 18 0z" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
                </svg>
              </div>
              <h1 className={styles.title}>Очікує підтвердження</h1>
              <p className={styles.subtitle}>Оплата обробляється банком. Ми надішлемо вам сповіщення, щойно отримаємо підтвердження платежу.</p>
            </>
          )}

          {/* Purchased Items */}
          {order && order.items && order.items.length > 0 && (
            <>
              <h3 className={styles.sectionTitle}>Товари в замовленні</h3>
              <div className={styles.ordersList}>
                {order.items.map((item) => (
                  <div className={styles.orderItem} key={item.orderItemId}>
                    <div className={styles.orderItemInfo}>
                      {item.productImage ? (
                        <img
                          src={item.productImage}
                          alt={item.productName}
                          className={styles.orderItemImage}
                        />
                      ) : (
                        <div className={styles.orderItemImage} style={{ display: "flex", alignItems: "center", justifyContent: "center", color: "#80838d", fontSize: "24px" }}>
                          📚
                        </div>
                      )}
                      <div className={styles.orderItemDetails}>
                        <p className={styles.orderItemAuthor}>Книга</p>
                        <p className={styles.orderItemBookTitle}>{item.productName}</p>
                      </div>
                    </div>
                    <div className={styles.orderItemBottom}>
                      <p className={styles.orderQty}>{item.quantity} шт.</p>
                      <p className={styles.orderPrice}>{Number(item.priceAtMoment)} ₴</p>
                    </div>
                  </div>
                ))}
              </div>
            </>
          )}

          {/* Delivery & Recipient Details */}
          {order && (
            <>
              <h3 className={styles.sectionTitle}>Дані доставки та оплати</h3>
              <div className={styles.detailRows}>
                <div className={styles.detailRow}>
                  <span className={styles.detailLabel}>Отримувач:</span>
                  <span className={styles.detailValue}>
                    {shippingAddress
                      ? `${shippingAddress.firstName} ${shippingAddress.lastName}`
                      : "—"}
                  </span>
                </div>
                <div className={styles.detailRow}>
                  <span className={styles.detailLabel}>Телефон:</span>
                  <span className={styles.detailValue}>{shippingAddress?.phone || "—"}</span>
                </div>
                <div className={styles.detailRow}>
                  <span className={styles.detailLabel}>Служба доставки:</span>
                  <span className={styles.detailValue}>{deliveryService}</span>
                </div>
                <div className={styles.detailRow}>
                  <span className={styles.detailLabel}>Адреса доставки:</span>
                  <span className={styles.detailValue}>
                    {shippingAddress
                      ? `${shippingAddress.country}, ${shippingAddress.state} обл., ${shippingAddress.city}, ${shippingAddress.street} (Індекс: ${shippingAddress.postalCode})`
                      : "—"}
                  </span>
                </div>
                <div className={styles.detailRow}>
                  <span className={styles.detailLabel}>Спосіб оплати:</span>
                  <span className={styles.detailValue}>
                    {order.paymentMethod === "Card" ? "Оплата карткою онлайн (LiqPay)" : order.paymentMethod}
                  </span>
                </div>
                {order.payment && (
                  <>
                    <div className={styles.detailRow}>
                      <span className={styles.detailLabel}>Статус платежу:</span>
                      <span className={`${styles.statusBadge} ${getStatusBadgeClass()}`}>
                        {getStatusText()}
                      </span>
                    </div>
                    {order.payment.transactionId && (
                      <div className={styles.detailRow}>
                        <span className={styles.detailLabel}>ID транзакції:</span>
                        <span className={styles.detailValue} style={{ fontFamily: "monospace", fontSize: "12px" }}>
                          {order.payment.transactionId}
                        </span>
                      </div>
                    )}
                  </>
                )}
                {order.notes && (
                  <div className={styles.detailRow} style={{ flexDirection: "column", alignItems: "flex-start", gap: "4px" }}>
                    <span className={styles.detailLabel}>Коментар до замовлення:</span>
                    <span className={styles.detailValue} style={{ fontStyle: "italic", textAlign: "left", width: "100%" }}>
                      {order.notes}
                    </span>
                  </div>
                )}
              </div>
            </>
          )}

          {/* Pricing Summary */}
          {order && (
            <>
              <h3 className={styles.sectionTitle}>Фінансовий підсумок</h3>
              <div className={styles.infoBox}>
                <div className={styles.infoRow}>
                  <span className={styles.infoLabel}>Номер замовлення</span>
                  <span className={`${styles.infoValue} ${styles.orderNum}`}>{orderNumber}</span>
                </div>
                <div className={styles.infoRow}>
                  <span className={styles.infoLabel}>Вартість товарів</span>
                  <span className={styles.infoValue}>{order.subtotal} ₴</span>
                </div>
                <div className={styles.infoRow}>
                  <span className={styles.infoLabel}>Вартість доставки</span>
                  <span className={styles.infoValue}>
                    {order.shippingCost > 0 ? `${order.shippingCost} ₴` : "Безкоштовно"}
                  </span>
                </div>
                {order.discountAmount > 0 && (
                  <div className={styles.infoRow}>
                    <span className={styles.infoLabel} style={{ color: "#f1c40f" }}>Знижка</span>
                    <span className={styles.infoValue} style={{ color: "#f1c40f" }}>-{order.discountAmount} ₴</span>
                  </div>
                )}
                {order.taxAmount > 0 && (
                  <div className={styles.infoRow}>
                    <span className={styles.infoLabel}>ПДВ</span>
                    <span className={styles.infoValue}>{order.taxAmount} ₴</span>
                  </div>
                )}
                <div className={styles.infoRow} style={{ marginTop: "12px", borderTop: "1px solid rgba(255, 255, 255, 0.08)", paddingTop: "12px" }}>
                  <span className={styles.infoLabel} style={{ fontSize: "16px", fontWeight: "600", color: "#ffffff" }}>Загалом до сплати</span>
                  <span className={styles.infoValue} style={{ fontSize: "18px", fontWeight: "600", color: "#ff007a" }}>
                    {order.totalPrice} ₴
                  </span>
                </div>
              </div>
            </>
          )}

          {/* Action buttons */}
          <div className={styles.actions}>
            {status === "failed" && order && (
              <Link href={`/me/orders/${order.orderId}`} className={styles.btnPrimary}>
                Спробувати знову
              </Link>
            )}
            {status !== "failed" && (
              <Link href="/me" className={styles.btnPrimary}>
                До моїх замовлень
              </Link>
            )}
            <Link href="/" className={styles.btnSecondary}>
              На головну
            </Link>
          </div>

        </div>
      </div>
    </PageLayout>
  );
}

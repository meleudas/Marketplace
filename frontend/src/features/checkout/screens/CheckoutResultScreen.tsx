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

        const paymentStatus = orderData.payment?.status;

        if (paymentStatus === "Completed") {
          setStatus("success");
          clearPoll();
        } else if (paymentStatus === "Failed") {
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

          {/* Order Details summary */}
          <div className={styles.infoBox}>
            <div className={styles.infoRow}>
              <span className={styles.infoLabel}>Номер замовлення</span>
              <span className={`${styles.infoValue} ${styles.orderNum}`}>{orderNumber}</span>
            </div>
            {order && (
              <>
                <div className={styles.infoRow}>
                  <span className={styles.infoLabel}>Сума до сплати</span>
                  <span className={styles.infoValue}>{order.totalPrice} ₴</span>
                </div>
                <div className={styles.infoRow}>
                  <span className={styles.infoLabel}>Спосіб оплати</span>
                  <span className={styles.infoValue}>Карта онлайн (LiqPay)</span>
                </div>
                <div className={styles.infoRow}>
                  <span className={styles.infoLabel}>Статус платежу</span>
                  <span className={`${styles.statusBadge} ${getStatusBadgeClass()}`}>
                    {getStatusText()}
                  </span>
                </div>
              </>
            )}
          </div>

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

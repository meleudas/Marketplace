"use client";

import Link from "next/link";
import { useEffect, useState, useCallback } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/features/auth/model/auth.store";
import { PageLayout } from "@/shared/ui/PageLayout";
import {
  ChevronLeftIcon,
} from "@/shared/ui";
import {
  fetchOrderDetails,
  OrderDetails,
} from "../api/me.api";
import {
  getStatusClass,
  getStatusLabel,
  type OrderStatusClassNames,
} from "../lib/order-status";
import styles from "./MeScreen.module.css";



interface MeOrderDetailScreenProps {
  orderId: number;
}

export function MeOrderDetailScreen({ orderId }: MeOrderDetailScreenProps) {
  const router = useRouter();
  const user = useAuth((state) => state.user);
  const isAuthenticated = useAuth((state) => state.isAuthenticated);
  const initialized = useAuth((state) => state.initialized);
  const loading = useAuth((state) => state.loading);
  const loadMe = useAuth((state) => state.loadMe);

  const [orderDetails, setOrderDetails] = useState<OrderDetails | null>(null);
  const [loadingDetails, setLoadingDetails] = useState(true);
  const [copied, setCopied] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void loadMe();
  }, [loadMe]);

  const loadDetails = useCallback(async () => {
    setLoadingDetails(true);
    setError(null);
    try {
      const details = await fetchOrderDetails(orderId);
      setOrderDetails(details);
    } catch (e) {
      console.error("Failed to load order details from API:", e);
      setError("Не вдалося завантажити деталі замовлення. Дані відсутні.");
    } finally {
      setLoadingDetails(false);
    }
  }, [orderId]);

  useEffect(() => {
    if (!isAuthenticated) {
      return;
    }

    const frameId = window.requestAnimationFrame(() => {
      void loadDetails();
    });

    return () => {
      window.cancelAnimationFrame(frameId);
    };
  }, [isAuthenticated, loadDetails]);

  if (!initialized || loading) {
    return (
      <PageLayout>
        <div className={styles.loadingContainer}>
          <p className={styles.loadingText}>Завантаження профілю...</p>
        </div>
      </PageLayout>
    );
  }

  if (!isAuthenticated || !user) {
    return (
      <PageLayout>
        <div className={styles.loadingContainer}>
          <div className={styles.authPrompt}>
            <h1 className={styles.authTitle}>Профіль</h1>
            <p className={styles.authSubtitle}>Увійдіть, щоб переглянути замовлення</p>
            <div className={styles.authActions}>
              <Link href="/auth/login" className={styles.signInButton}>
                Увійти
              </Link>
              <Link href="/" className={styles.backButton}>
                На головну
              </Link>
            </div>
          </div>
        </div>
      </PageLayout>
    );
  }

  const handleBack = () => {
    router.push("/me/orders");
  };

  // Determine delivery service
  const shippingAddress = orderDetails?.addresses.find((a) => a.kind === "Shipping");
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

  const isPaymentSuccess =
    orderDetails?.payment?.status?.toLowerCase() === "captured" ||
    orderDetails?.payment?.status?.toLowerCase() === "completed" ||
    orderDetails?.payment?.status?.toLowerCase() === "success" ||
    orderDetails?.payment?.status?.toLowerCase() === "paid";
  
  const isPaymentFailed =
    orderDetails?.payment?.status?.toLowerCase() === "failed" ||
    orderDetails?.payment?.status?.toLowerCase() === "failure";

  const getPaymentStatusText = () => {
    if (isPaymentSuccess) return "Сплачено успішно";
    if (isPaymentFailed) return "Помилка оплати";
    return orderDetails?.payment?.status || "Очікує оплати";
  };

  // Build carrier tracking link
  const trackingNumber = orderDetails?.trackingNumber;
  let trackingLink = "";
  if (trackingNumber) {
    if (isNovaPoshta) {
      trackingLink = `https://novaposhta.ua/tracking/?cargo_number=${trackingNumber}`;
    } else if (isUkrPoshta) {
      trackingLink = `https://track.ukrposhta.ua/tracking_UA.html?barcode=${trackingNumber}`;
    }
  }

  const copyTracking = () => {
    if (trackingNumber) {
      void navigator.clipboard.writeText(trackingNumber);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  return (
    <PageLayout className={styles.mainContainer}>
      <div className={styles.page}>
        <div className={`${styles.main} ${styles.singleColumnPage}`}>
          <button type="button" onClick={handleBack} className={styles.backNav} style={{ background: "none", border: "none", width: "auto" }}>
            <ChevronLeftIcon className={styles.backNavIcon} />
            <span className={styles.backNavTitle}>До замовлень</span>
          </button>

          <section className={styles.card}>
            {loadingDetails ? (
              <div className={styles.detailLoading}>Завантаження деталей замовлення...</div>
            ) : error || !orderDetails ? (
              <div className={styles.detailLoading} style={{ color: "#e74c3c", padding: "40px 0" }}>
                {error || "Деталі замовлення відсутні."}
              </div>
            ) : (
              <>
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", flexWrap: "wrap", gap: "12px", marginBottom: "20px" }}>
                  <h2 className={styles.sectionTitle} style={{ margin: 0 }}>
                    Замовлення №{orderDetails.orderNumber}
                  </h2>
                  <span className={`${styles.orderStatusBadge} ${getStatusClass(orderDetails.status, styles as unknown as OrderStatusClassNames)}`}>
                    {getStatusLabel(orderDetails.status)}
                  </span>
                </div>

                <div style={{ color: "#80838d", fontSize: "14px", marginBottom: "24px", fontFamily: "var(--font-primary)" }}>
                  Створено: {new Date(orderDetails.createdAt).toLocaleString("uk-UA")}
                </div>

                {trackingNumber && (
                  <div className={styles.trackingSection}>
                    <div className={styles.trackingHeader}>
                      <span className={styles.trackingTitle}>Відстеження посилки</span>
                      <span className={styles.trackingCarrier}>{deliveryService}</span>
                    </div>
                    <div className={styles.trackingBody}>
                      <span className={styles.trackingNumberText}>{trackingNumber}</span>
                      <div style={{ display: "flex", gap: "8px" }}>
                        <button
                          type="button"
                          onClick={copyTracking}
                          className={styles.trackButton}
                          style={{ background: "#2a2927", border: "1px solid #5f7081" }}
                        >
                          {copied ? "Скопійовано!" : "Копіювати"}
                        </button>
                        {trackingLink && (
                          <a
                            href={trackingLink}
                            target="_blank"
                            rel="noopener noreferrer"
                            className={styles.trackButton}
                          >
                            Відстежити
                          </a>
                        )}
                      </div>
                    </div>
                  </div>
                )}

                <h3 className={styles.sectionTitle} style={{ marginTop: "28px", fontSize: "18px" }}>Товари в замовленні</h3>
                <div className={styles.ordersList} style={{ marginTop: "12px" }}>
                  {orderDetails.items.map((item) => (
                    <Link
                      href={`/products/${item.productId}`}
                      className={styles.orderItemLink}
                      key={item.orderItemId}
                    >
                      <div className={styles.orderItem} style={{ borderBottom: "1px solid #2a2927", paddingBottom: "16px", marginBottom: "16px" }}>
                        <div className={styles.orderItemTop}>
                          <div className={styles.orderItemInfo}>
                            <div className={styles.orderItemImage} />
                            <div className={styles.orderItemDetails}>
                              <p className={styles.orderItemAuthor}>Книга</p>
                              <p className={styles.orderItemAvailability}>В наявності</p>
                              <p className={styles.orderItemBookTitle} style={{ color: "#ee0290", textDecoration: "underline" }}>
                                {item.productName}
                              </p>
                            </div>
                          </div>
                        </div>
                        <div className={styles.orderItemBottom}>
                          <p className={styles.orderQty}>{item.quantity}шт.</p>
                          <p className={styles.orderPrice}>
                            {item.discount > 0 ? (
                              <>
                                <span style={{ textDecoration: "line-through", color: "#80838d", marginRight: "8px", fontSize: "14px" }}>
                                  {Number(item.priceAtMoment)} грн.
                                </span>
                                <span>{Number(item.totalPrice / item.quantity)} грн.</span>
                              </>
                            ) : (
                              <span>{Number(item.priceAtMoment)} грн.</span>
                            )}
                          </p>
                        </div>
                      </div>
                    </Link>
                  ))}
                </div>

                <div style={{ marginTop: "24px", borderBottom: "1px solid #2a2927", paddingBottom: "16px" }}>
                  <div className={styles.summaryRow} style={{ marginBottom: "8px" }}>
                    <p className={styles.summaryLabel}>Вартість товарів</p>
                    <p className={styles.summaryValue}>{orderDetails.subtotal} грн.</p>
                  </div>
                  <div className={styles.summaryRow} style={{ marginBottom: "8px" }}>
                    <p className={styles.summaryLabel}>Вартість доставки</p>
                    <p className={styles.summaryValue}>
                      {orderDetails.shippingCost > 0 ? `${orderDetails.shippingCost} грн.` : "Безкоштовно"}
                    </p>
                  </div>
                  {orderDetails.discountAmount > 0 && (
                    <div className={styles.summaryRow} style={{ marginBottom: "8px" }}>
                      <p className={styles.summaryLabel} style={{ color: "#ffc107" }}>Знижка</p>
                      <p className={styles.summaryValue} style={{ color: "#ffc107" }}>-{orderDetails.discountAmount} грн.</p>
                    </div>
                  )}
                  {orderDetails.taxAmount > 0 && (
                    <div className={styles.summaryRow} style={{ marginBottom: "8px" }}>
                      <p className={styles.summaryLabel}>ПДВ</p>
                      <p className={styles.summaryValue}>{orderDetails.taxAmount} грн.</p>
                    </div>
                  )}
                  <div className={styles.summaryRow} style={{ marginTop: "12px", borderTop: "1px solid #2a2927", paddingTop: "12px" }}>
                    <p className={styles.summaryLabel} style={{ fontSize: "18px", fontWeight: "600", color: "#ffffff" }}>Загалом</p>
                    <p className={styles.summaryValue} style={{ fontSize: "18px", fontWeight: "600", color: "#ee0290" }}>{orderDetails.totalPrice} грн.</p>
                  </div>
                </div>

                <h3 className={styles.sectionTitle} style={{ marginTop: "28px", fontSize: "18px" }}>Дані доставки та оплати</h3>
                <div className={styles.detailRows} style={{ marginTop: "12px" }}>
                  <div className={styles.detailRow}>
                    <p className={styles.detailLabel}>Отримувач:</p>
                    <p className={styles.detailValue}>
                      {shippingAddress
                        ? `${shippingAddress.firstName} ${shippingAddress.lastName}`
                        : user
                          ? `${user.firstName} ${user.lastName}`
                          : "Не вказано"}
                    </p>
                  </div>
                  <div className={styles.detailRow}>
                    <p className={styles.detailLabel}>Телефон отримувача:</p>
                    <p className={styles.detailValue}>
                      {shippingAddress?.phone || "Не вказано"}
                    </p>
                  </div>
                  <div className={styles.detailRow}>
                    <p className={styles.detailLabel}>Спосіб оплати:</p>
                    <p className={styles.detailValue}>
                      {orderDetails.paymentMethod === "Card" ? "Оплата карткою онлайн" : orderDetails.paymentMethod}
                    </p>
                  </div>
                  {orderDetails.payment && (
                    <>
                      <div className={styles.detailRow}>
                        <p className={styles.detailLabel}>Статус платежу:</p>
                        <p className={styles.detailValue} style={{ color: isPaymentSuccess ? "#28a745" : isPaymentFailed ? "#dc3545" : "#ffc107" }}>
                          {getPaymentStatusText()}
                        </p>
                      </div>
                      {orderDetails.payment.transactionId && (
                        <div className={styles.detailRow}>
                          <p className={styles.detailLabel}>ID транзакції:</p>
                          <p className={styles.detailValue} style={{ fontFamily: "monospace" }}>
                            {orderDetails.payment.transactionId}
                          </p>
                        </div>
                      )}
                    </>
                  )}
                  <div className={styles.detailRow}>
                    <p className={styles.detailLabel}>Служба доставки:</p>
                    <p className={styles.detailValue}>{deliveryService}</p>
                  </div>
                  <div className={styles.detailRow}>
                    <p className={styles.detailLabel}>Адреса доставки:</p>
                    <p className={styles.detailValue}>
                      {shippingAddress
                        ? `${shippingAddress.country}, ${shippingAddress.state} обл., ${shippingAddress.city}, ${shippingAddress.street} (Індекс: ${shippingAddress.postalCode})`
                        : "Не вказано"}
                    </p>
                  </div>
                  {orderDetails.notes && (
                    <div className={styles.detailRow} style={{ marginTop: "8px" }}>
                      <p className={styles.detailLabel}>Коментар до замовлення:</p>
                      <p className={styles.detailValue} style={{ fontStyle: "italic" }}>{orderDetails.notes}</p>
                    </div>
                  )}
                </div>

                {orderDetails.statusHistory && orderDetails.statusHistory.length > 0 && (
                  <>
                    <h3 className={styles.sectionTitle} style={{ marginTop: "28px", fontSize: "18px" }}>Історія замовлення</h3>
                    <div className={styles.timeline}>
                      {orderDetails.statusHistory.map((history, idx) => {
                        const isLatest = idx === orderDetails.statusHistory.length - 1;
                        return (
                          <div className={styles.timelineItem} key={idx}>
                            <span className={`${styles.timelineDot} ${isLatest ? styles.timelineDotActive : ""}`} />
                            <div className={styles.timelineContent}>
                              <span className={styles.timelineStatus}>
                                {getStatusLabel(history.newStatus)}
                              </span>
                              <span className={styles.timelineDate}>
                                {new Date(history.changedAt).toLocaleString("uk-UA")}
                              </span>
                              {history.comment && (
                                <span className={styles.timelineComment}>{history.comment}</span>
                              )}
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  </>
                )}

                {orderDetails.returns && orderDetails.returns.length > 0 && (
                  <>
                    <h3 className={styles.sectionTitle} style={{ marginTop: "28px", fontSize: "18px", color: "#dc3545" }}>Повернення</h3>
                    <div style={{ marginTop: "12px" }}>
                      {orderDetails.returns.map((ret) => (
                        <div key={ret.returnId} style={{ background: "rgba(220, 53, 69, 0.05)", border: "1px solid rgba(220, 53, 69, 0.2)", borderRadius: "8px", padding: "16px", marginBottom: "12px" }}>
                          <div style={{ display: "flex", justifyContent: "space-between", marginBottom: "8px" }}>
                            <span style={{ fontWeight: "600", color: "#ffffff" }}>Запит на повернення №{ret.returnId}</span>
                            <span style={{ color: "#dc3545", fontWeight: "500" }}>{ret.status}</span>
                          </div>
                          <p style={{ margin: "0 0 8px", fontSize: "14px", color: "#80838d" }}>Причина: {ret.reasonCode}</p>
                          <p style={{ margin: "0", fontSize: "12px", color: "#80838d" }}>Створено: {new Date(ret.createdAt).toLocaleDateString("uk-UA")}</p>
                        </div>
                      ))}
                    </div>
                  </>
                )}

                <div className={styles.supportSection}>
                  <h4 className={styles.supportTitle}>Потрібна допомога із замовленням?</h4>
                  <div className={styles.supportButtons}>
                    <a
                      href="https://t.me/booktop_support"
                      target="_blank"
                      rel="noopener noreferrer"
                      className={`${styles.supportBtn} ${styles.btnManager}`}
                    >
                      Написати в Telegram
                    </a>
                    <a
                      href="tel:+380800333444"
                      className={`${styles.supportBtn} ${styles.btnSupport}`}
                    >
                      Гаряча лінія підтримки
                    </a>
                  </div>
                </div>
              </>
            )}
          </section>
        </div>
      </div>
    </PageLayout>
  );
}

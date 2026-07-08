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
  OrderStatusHistoryDto,
} from "../api/me.api";
import styles from "./MeScreen.module.css";

const MOCK_ORDER_DETAIL: OrderDetails = {
  orderId: 1,
  orderNumber: "2345678901354",
  customerId: "",
  companyId: "",
  status: "Delivered",
  totalPrice: 1900,
  subtotal: 1900,
  shippingCost: 0,
  discountAmount: 0,
  taxAmount: 0,
  paymentMethod: "Card",
  notes: "Будь ласка, запакуйте книгу надійно як подарунок.",
  trackingNumber: "20450849204918",
  shippedAt: "2026-12-09T13:30:00Z",
  deliveredAt: "2026-12-09T15:00:00Z",
  cancelledAt: null,
  refundedAt: null,
  createdAt: "2026-12-09T12:00:00Z",
  updatedAt: "2026-12-09T15:00:00Z",
  items: [
    {
      orderItemId: 1,
      productId: 101,
      productName: "Портрет Доріана Грея",
      productImage: null,
      quantity: 1,
      priceAtMoment: 950,
      discount: 0,
      totalPrice: 950,
    },
    {
      orderItemId: 2,
      productId: 102,
      productName: "Портрет Доріана Грея",
      productImage: null,
      quantity: 1,
      priceAtMoment: 950,
      discount: 0,
      totalPrice: 950,
    },
  ],
  addresses: [
    {
      kind: "Shipping",
      firstName: "Данило",
      lastName: "Гамаран",
      phone: "+380 56 435 678",
      street: "вул. Сагайдачного 54ж",
      city: "м. Чернівці",
      state: "Чернівецька",
      postalCode: "58000",
      country: "Україна",
    },
  ],
  payment: {
    paymentId: 12,
    method: "Card",
    amount: 1900,
    currency: "UAH",
    transactionId: "pay_txn_8482049810",
    status: "Captured",
    processedAt: "2026-12-09T12:05:00Z",
  },
  refunds: [],
  returns: [],
  statusHistory: [
    {
      oldStatus: "Created",
      newStatus: "Pending",
      changedByUserId: "00000000-0000-0000-0000-000000000000",
      actorRole: "System",
      source: "Checkout",
      comment: "Замовлення успішно створене в системі.",
      correlationId: null,
      changedAt: "2026-12-09T12:00:00Z",
    },
    {
      oldStatus: "Pending",
      newStatus: "Processing",
      changedByUserId: "00000000-0000-0000-0000-000000000000",
      actorRole: "System",
      source: "PaymentService",
      comment: "Оплата отримана.",
      correlationId: null,
      changedAt: "2026-12-09T12:05:00Z",
    },
    {
      oldStatus: "Processing",
      newStatus: "Shipped",
      changedByUserId: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      actorRole: "Seller",
      source: "FulfillmentModule",
      comment: "Передано кур'єру.",
      correlationId: null,
      changedAt: "2026-12-09T13:30:00Z",
    },
    {
      oldStatus: "Shipped",
      newStatus: "Delivered",
      changedByUserId: "00000000-0000-0000-0000-000000000000",
      actorRole: "System",
      source: "ShippingCarrierIntegration",
      comment: "Посилку успішно отримано одержувачем.",
      correlationId: null,
      changedAt: "2026-12-09T15:00:00Z",
    },
  ],
};

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

  useEffect(() => {
    void loadMe();
  }, [loadMe]);

  const loadDetails = useCallback(async () => {
    setLoadingDetails(true);
    try {
      if (orderId === 1 || orderId === 2) {
        setOrderDetails({
          ...MOCK_ORDER_DETAIL,
          orderId,
          orderNumber: orderId === 1 ? "2345678901354" : "2345678901355",
          status: orderId === 1 ? "Delivered" : "Shipped",
          totalPrice: orderId === 1 ? 1900 : 950,
          subtotal: orderId === 1 ? 1900 : 950,
          trackingNumber: orderId === 1 ? "20450849204918" : "59000849204919",
          items: orderId === 1 ? MOCK_ORDER_DETAIL.items : [MOCK_ORDER_DETAIL.items[0]],
          statusHistory: orderId === 1
            ? MOCK_ORDER_DETAIL.statusHistory
            : MOCK_ORDER_DETAIL.statusHistory.slice(0, 3),
        });
        setLoadingDetails(false);
        return;
      }

      const details = await fetchOrderDetails(orderId);
      setOrderDetails(details);
    } catch (e) {
      console.warn("Failed to load order details from API, using mock fallback:", e);
      setOrderDetails({
        ...MOCK_ORDER_DETAIL,
        orderId,
      });
    } finally {
      setLoadingDetails(false);
    }
  }, [orderId]);

  useEffect(() => {
    if (isAuthenticated) {
      void loadDetails();
    }
  }, [isAuthenticated, loadDetails]);

  const getNormalizedStatus = (status: any): string => {
    if (status === null || status === undefined) return "";
    if (typeof status === "number") {
      switch (status) {
        case 0: return "pending";
        case 1: return "processing";
        case 2: return "shipped";
        case 3: return "delivered";
        case 4: return "cancelled";
        case 5: return "refunded";
        default: return String(status).toLowerCase();
      }
    }
    return String(status).toLowerCase();
  };

  const getStatusLabel = (status: any) => {
    const normalized = getNormalizedStatus(status);
    switch (normalized) {
      case "pending": return "Очікує оплати";
      case "processing": return "Обробляється";
      case "shipped": return "Відправлено";
      case "delivered": return "Доставлено";
      case "cancelled": return "Скасовано";
      case "refunded": return "Повернено";
      default: return String(status);
    }
  };

  const getStatusClass = (status: any) => {
    const normalized = getNormalizedStatus(status);
    switch (normalized) {
      case "pending": return styles.statusPending;
      case "processing": return styles.statusProcessing;
      case "shipped": return styles.statusShipped;
      case "delivered": return styles.statusDelivered;
      case "cancelled": return styles.statusCancelled;
      case "refunded": return styles.statusRefunded;
      default: return "";
    }
  };

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
              <Link href="/auth" className={styles.signInButton}>
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
            {loadingDetails || !orderDetails ? (
              <div className={styles.detailLoading}>Завантаження деталей замовлення...</div>
            ) : (
              <>
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", flexWrap: "wrap", gap: "12px", marginBottom: "20px" }}>
                  <h2 className={styles.sectionTitle} style={{ margin: 0 }}>
                    Замовлення №{orderDetails.orderNumber}
                  </h2>
                  <span className={`${styles.orderStatusBadge} ${getStatusClass(orderDetails.status)}`}>
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
                        : `${user.firstName} ${user.lastName}`}
                    </p>
                  </div>
                  <div className={styles.detailRow}>
                    <p className={styles.detailLabel}>Телефон отримувача:</p>
                    <p className={styles.detailValue}>
                      {shippingAddress?.phone || "+380 56 435 678"}
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
                        <p className={styles.detailValue} style={{ color: orderDetails.payment.status === "Captured" ? "#28a745" : "#ffc107" }}>
                          {orderDetails.payment.status === "Captured" ? "Сплачено успішно" : orderDetails.payment.status}
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
                        : "Чернівці"}
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

"use client";

import Link from "next/link";
import { useEffect, useState, useCallback } from "react";
import { useAuth } from "@/features/auth/model/auth.store";
import { PageLayout } from "@/shared/ui/PageLayout";
import {
  ChevronLeftIcon,
  SleapCat,
} from "@/shared/ui";
import {
  fetchOrders,
  OrderListItem,
} from "../api/me.api";
import {
  getNormalizedStatus,
  getStatusClass,
  getStatusLabel,
  type OrderStatusClassNames,
} from "../lib/order-status";
import styles from "./MeScreen.module.css";

type OrderTab = "all" | "active" | "completed";

export function MeOrdersScreen() {
  const user = useAuth((state) => state.user);
  const isAuthenticated = useAuth((state) => state.isAuthenticated);
  const initialized = useAuth((state) => state.initialized);
  const loading = useAuth((state) => state.loading);
  const loadMe = useAuth((state) => state.loadMe);

  const [orders, setOrders] = useState<OrderListItem[]>([]);
  const [activeTab, setActiveTab] = useState<OrderTab>("all");

  const loadOrdersData = useCallback(async () => {
    try {
      const pagedOrders = await fetchOrders();
      setOrders(pagedOrders.items);
    } catch (e) {
      console.warn("Failed to fetch orders from API:", e);
    }
  }, []);

  useEffect(() => {
    void loadMe();
  }, [loadMe]);

  useEffect(() => {
    if (!isAuthenticated) {
      return;
    }

    const frameId = window.requestAnimationFrame(() => {
      void loadOrdersData();
    });

    return () => {
      window.cancelAnimationFrame(frameId);
    };
  }, [isAuthenticated, loadOrdersData]);

  const hasApiOrders = orders && orders.length > 0;

  const displayOrdersList = hasApiOrders
    ? orders
    : [
        {
          orderId: 1,
          orderNumber: "2345678901354",
          status: "Delivered",
          totalPrice: 1900,
          createdAt: "2026-12-09T12:00:00Z",
          customerId: "",
          companyId: "",
          paymentMethod: "Card",
          updatedAt: "",
        },
        {
          orderId: 2,
          orderNumber: "2345678901355",
          status: "Shipped",
          totalPrice: 950,
          createdAt: "2026-12-10T14:30:00Z",
          customerId: "",
          companyId: "",
          paymentMethod: "Card",
          updatedAt: "",
        },
      ];

  const filteredOrders = displayOrdersList.filter((order) => {
    if (activeTab === "all") return true;
    const normalized = getNormalizedStatus(order.status);
    const isActive = ["pending", "processing", "shipped"].includes(normalized);
    const isCompleted = ["delivered", "cancelled", "refunded"].includes(normalized);
    return activeTab === "active" ? isActive : isCompleted;
  });

  if (!initialized || loading) {
    return (
      <PageLayout>
        <div className={styles.loadingContainer}>
          <p className={styles.loadingText}>Завантаження історії замовлень...</p>
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

  return (
    <PageLayout className={styles.mainContainer}>
      <div className={styles.page}>
        <div className={`${styles.main} ${styles.singleColumnPage}`}>
          <Link href="/me" className={styles.backNav}>
            <ChevronLeftIcon className={styles.backNavIcon} />
            <span className={styles.backNavTitle}>До профілю</span>
          </Link>

          <section className={styles.card}>
            <SleapCat className={styles.catIllustration} />
            <h2 className={styles.sectionTitle}>Історія замовлень</h2>

            <div className={styles.tabsRow}>
              <button
                type="button"
                className={`${styles.tab} ${activeTab === "all" ? styles.tabActive : ""}`}
                onClick={() => setActiveTab("all")}
              >
                Усі
              </button>
              <button
                type="button"
                className={`${styles.tab} ${activeTab === "active" ? styles.tabActive : ""}`}
                onClick={() => setActiveTab("active")}
              >
                Активні
              </button>
              <button
                type="button"
                className={`${styles.tab} ${activeTab === "completed" ? styles.tabActive : ""}`}
                onClick={() => setActiveTab("completed")}
              >
                Завершені
              </button>
            </div>

            <div className={styles.ordersList} style={{ marginTop: "12px" }}>
              {filteredOrders.length > 0 ? (
                filteredOrders.map((order) => (
                  <Link
                    key={order.orderId}
                    href={`/me/orders/${order.orderId}`}
                    className={styles.orderListButton}
                    style={{ textDecoration: "none" }}
                  >
                    <div className={styles.orderButtonLeft}>
                      <span className={styles.orderButtonNumber}>№ {order.orderNumber}</span>
                      <span className={styles.orderButtonDate}>
                        {new Date(order.createdAt).toLocaleDateString("uk-UA")}
                      </span>
                    </div>
                    <div className={styles.orderButtonRight}>
                      <span className={styles.orderButtonPrice}>{order.totalPrice} грн.</span>
                      <span className={`${styles.orderStatusBadge} ${getStatusClass(order.status, styles as unknown as OrderStatusClassNames)}`}>
                        {getStatusLabel(order.status)}
                      </span>
                    </div>
                  </Link>
                ))
              ) : (
                <div className={styles.noOrdersText}>Замовлень немає</div>
              )}
            </div>
          </section>
        </div>
      </div>
    </PageLayout>
  );
}

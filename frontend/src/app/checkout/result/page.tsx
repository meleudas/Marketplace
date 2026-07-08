import { CheckoutResultScreen } from "@/features/checkout/screens/CheckoutResultScreen";
import { Suspense } from "react";
import { PageLayout, Spinner } from "@/shared/ui";
import styles from "@/features/checkout/screens/CheckoutResultScreen.module.css";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Результат оплати | Booktop",
  description: "Статус оплати вашого замовлення",
};

export default function CheckoutResultPage() {
  return (
    <Suspense
      fallback={
        <PageLayout>
          <div className={styles.root}>
            <div className={styles.card}>
              <div className={styles.spinnerWrapper}>
                <Spinner size="lg" />
                <p className={styles.spinnerText}>Завантаження сторінки результату...</p>
              </div>
            </div>
          </div>
        </PageLayout>
      }
    >
      <CheckoutResultScreen />
    </Suspense>
  );
}

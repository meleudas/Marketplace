import { CheckoutScreen } from "@/features/checkout/screens/CheckoutScreen";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Оформлення замовлення",
  description: "Оформлення замовлення в інтернет-магазині Booktop",
};

export default function CheckoutPage() {
  return <CheckoutScreen />;
}

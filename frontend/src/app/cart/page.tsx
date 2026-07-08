import { CartScreen } from "@/features/cart/screens/CartScreen";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Кошик",
  description: "Кошик покупок в інтернет-магазині Booktop",
};

export default function CartPage() {
  return <CartScreen />;
}

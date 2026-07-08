import type { Metadata } from "next";
import { MeOrdersScreen } from "@/features/me/screens/MeOrdersScreen";

export const metadata: Metadata = {
  title: "Мої замовлення",
  description: "Повний список ваших замовлень на Booktop з можливістю фільтрації за статусом та перегляду детальної інформації.",
};

export default function Page() {
  return <MeOrdersScreen />;
}

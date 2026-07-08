import type { Metadata } from "next";
import { MeScreen } from "@/features/me/screens/MeScreen";

export const metadata: Metadata = {
  title: "Мій профіль",
  description: "Особистий кабінет користувача Booktop. Перегляд замовлень, адрес доставки та сертифікатів.",
};

export default function Page() {
  return <MeScreen />;
}

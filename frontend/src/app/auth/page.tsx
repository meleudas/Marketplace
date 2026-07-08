import type { Metadata } from "next";
import { AuthHomeScreen } from "@/features/auth/screens/AuthHomeScreen";

export const metadata: Metadata = {
  title: "Вхід та реєстрація",
  description: "Авторизуйтесь або зареєструйтесь на Booktop, щоб отримати доступ до особистого кабінету, кошика та замовлень.",
};

export default function Page() {
  return <AuthHomeScreen />;
}

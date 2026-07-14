import type { Metadata } from "next";
import { Suspense } from "react";
import { AuthHomeScreen } from "@/features/auth/screens/AuthHomeScreen";

export const metadata: Metadata = {
  title: "Вхід та реєстрація",
  description: "Авторизуйтесь або зареєструйтесь на Booktop, щоб отримати доступ до особистого кабінету, кошика та замовлень.",
};

export default function Page() {
  return (
    <Suspense fallback={null}>
      <AuthHomeScreen />
    </Suspense>
  );
}

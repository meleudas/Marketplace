import type { Metadata } from "next";
import { Suspense } from "react";
import { RegisterScreen } from "@/features/auth/screens/RegisterScreen";

export const metadata: Metadata = {
  title: "Реєстрація | Book Top",
  description: "Створіть акаунт Book Top для замовлення книг, відстеження доставки та отримання персональних пропозицій.",
};

export default function RegisterPage() {
  return (
    <Suspense fallback={null}>
      <RegisterScreen />
    </Suspense>
  );
}

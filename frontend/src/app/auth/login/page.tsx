import type { Metadata } from "next";
import { Suspense } from "react";
import { LoginScreen } from "@/features/auth/screens/LoginScreen";

export const metadata: Metadata = {
  title: "Вхід | Book Top",
  description: "Увійдіть до свого акаунта Book Top для перегляду замовлень, кошика та персональних пропозицій.",
};

export default function LoginPage() {
  return (
    <Suspense fallback={null}>
      <LoginScreen />
    </Suspense>
  );
}

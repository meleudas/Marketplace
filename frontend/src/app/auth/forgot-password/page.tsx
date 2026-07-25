import type { Metadata } from "next";
import { Suspense } from "react";
import { ForgotPasswordScreen } from "@/features/auth/screens/ForgotPasswordScreen";

export const metadata: Metadata = {
  title: "Відновлення пароля | Book Top",
  description: "Скиньте пароль Book Top за допомогою електронної пошти та коду підтвердження.",
};

export default function ForgotPasswordPage() {
  return (
    <Suspense fallback={null}>
      <ForgotPasswordScreen />
    </Suspense>
  );
}

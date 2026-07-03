import { Suspense } from "react";
import { ConfirmEmailScreen } from "@/features/confirm-email/screens/ConfirmEmailScreen";

export default function Page() {
  return (
    <Suspense fallback={null}>
      <ConfirmEmailScreen />
    </Suspense>
  );
}

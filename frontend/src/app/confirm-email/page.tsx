import { Suspense } from "react";
import { ConfirmEmailScreen } from "@/features/auth/screens/ConfirmEmailScreen";

export default function Page() {
  return (
    <Suspense fallback={null}>
      <ConfirmEmailScreen />
    </Suspense>
  );
}


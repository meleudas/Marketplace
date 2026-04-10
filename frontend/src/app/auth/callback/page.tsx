import { Suspense } from "react";
import { GoogleAuthCallbackScreen } from "@/features/auth/screens/GoogleAuthCallbackScreen";

export default function Page() {
  return (
    <Suspense fallback={null}>
      <GoogleAuthCallbackScreen />
    </Suspense>
  );
}



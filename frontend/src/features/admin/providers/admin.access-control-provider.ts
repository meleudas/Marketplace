import type { AccessControlProvider } from "@refinedev/core";
import { useAuth } from "@/features/auth/model/auth.store";

const isAdmin = (): boolean => {
  return useAuth.getState().user?.role === "admin";
};

export const adminAccessControlProvider: AccessControlProvider = {
  can: async () => {
    const allowed = isAdmin();

    return {
      can: allowed,
      reason: allowed ? undefined : "Only admin can access this section.",
    };
  },
};


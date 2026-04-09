import type { AuthProvider } from "@refinedev/core";
import { useAuth } from "@/features/auth/model/auth.store";

export const adminAuthProvider: AuthProvider = {
  login: async () => {
    return {
      success: true,
    };
  },

  logout: async () => {
    await useAuth.getState().logout();

    return {
      success: true,
      redirectTo: "/",
    };
  },

  check: async () => {
    await useAuth.getState().loadMe();
    const { isAuthenticated } = useAuth.getState();

    if (isAuthenticated) {
      return {
        authenticated: true,
      };
    }

    return {
      authenticated: false,
      redirectTo: "/",
      error: {
        message: "Authentication required",
        name: "Unauthorized",
      },
    };
  },

  getPermissions: async () => {
    const user = useAuth.getState().user;
    return user?.role ?? null;
  },

  getIdentity: async () => {
    const user = useAuth.getState().user;

    if (!user) {
      return null;
    }

    return {
      id: user.id,
      name: `${user.firstName} ${user.lastName}`,
      avatar: user.avatar ?? undefined,
    };
  },

  onError: async () => {
    return {};
  },
};


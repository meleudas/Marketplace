"use client";

import { Refine } from "@refinedev/core";
import { adminAccessControlProvider } from "@/features/admin/providers/admin.access-control-provider";
import { adminAuthProvider } from "@/features/admin/providers/admin.auth-provider";
import { adminDataProvider } from "@/features/admin/providers/admin.data-provider";

interface AdminRefineProviderProps {
  children: React.ReactNode;
}

export function AdminRefineProvider({ children }: AdminRefineProviderProps) {
  return (
    <Refine
      dataProvider={adminDataProvider}
      authProvider={adminAuthProvider}
      accessControlProvider={adminAccessControlProvider}
      resources={[
        {
          name: "companies",
          list: "/admin/companies",
          create: "/admin/companies/create",
          edit: "/admin/companies/:id/edit",
        },
        {
          name: "companies-pending",
          list: "/admin/companies/pending",
        },
        {
          name: "categories",
          list: "/admin/categories",
          create: "/admin/categories/create",
          edit: "/admin/categories/:id/edit",
        },
        {
          name: "categories-active",
          list: "/admin/categories/active",
        },
      ]}
      options={{
        syncWithLocation: false,
      }}
    >
      {children}
    </Refine>
  );
}


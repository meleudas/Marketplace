import type { Metadata } from "next";
import { AdminShell } from "@/features/admin/ui/AdminShell";

export const metadata: Metadata = {
  title: "Marketplace Admin",
  description: "Admin section powered by Refine",
};

export default function AdminLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return <AdminShell>{children}</AdminShell>;
}


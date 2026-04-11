import { WorkspaceLayout } from "@/features/workspace/ui/WorkspaceLayout";

export default function Layout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return <WorkspaceLayout>{children}</WorkspaceLayout>;
}


import { CompaniesEditScreen } from "@/features/admin/screens/CompaniesEditScreen";

interface PageProps {
  params: Promise<{ id: string }>;
}

export default async function Page({ params }: PageProps) {
  const { id } = await params;

  return <CompaniesEditScreen id={id} />;
}


import { CategoriesEditScreen } from "@/features/admin/screens/CategoriesEditScreen";

interface PageProps {
  params: Promise<{ id: string }>;
}

export default async function Page({ params }: PageProps) {
  const { id } = await params;

  return <CategoriesEditScreen id={Number(id)} />;
}


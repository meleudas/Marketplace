import { CompanyDetailsScreen } from "@/features/companies/[slug]/screens/CompanyDetailsScreen";

interface CompanyPageProps {
  params: Promise<{ slug: string }>;
}

export default async function Page({ params }: CompanyPageProps) {
  const { slug } = await params;

  return <CompanyDetailsScreen slug={slug} />;
}

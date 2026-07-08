import type { Metadata } from "next";
import { MeOrderDetailScreen } from "@/features/me/screens/MeOrderDetailScreen";

interface PageProps {
  params: Promise<{ id: string }>;
}

export const metadata: Metadata = {
  title: "Деталі замовлення",
  description: "Детальна інформація про замовлення на Booktop: статус, склад товарів, вартість, оплата та доставка.",
};

export default async function Page({ params }: PageProps) {
  const resolvedParams = await params;
  return <MeOrderDetailScreen orderId={Number(resolvedParams.id)} />;
}

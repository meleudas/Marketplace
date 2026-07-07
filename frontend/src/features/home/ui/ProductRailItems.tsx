import Link from "next/link";
import { ProductCard } from "@/shared/ui";
import type { ProductRailCard } from "../lib/map-product-to-rail-card";
import styles from "./RecommendationsRail.module.css";

interface ProductRailItemsProps {
  items: ProductRailCard[];
}

export function ProductRailItems({ items }: ProductRailItemsProps) {
  return items.map((item) => (
    <Link key={item.id} href={item.href} className={styles.cardLink} role="listitem">
      <ProductCard product={item} />
    </Link>
  ));
}

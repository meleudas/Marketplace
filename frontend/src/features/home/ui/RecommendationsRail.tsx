"use client";

import { Children, useState } from "react";
import {
  Button,
  ChevronDownIcon,
  PercentIcon,
  PlusIcon,
  ProductCardSkeleton,
  StarIcon,
  UserIcon,
} from "@/shared/ui";
import iconStyles from "@/shared/ui/icons/Icon.module.css";
import styles from "./RecommendationsRail.module.css";
import type { ComponentType } from "react";
import type { IconProps } from "@/shared/ui";
import type { ReactNode } from "react";

export type RecommendationsRailVariant = "personal" | "popular" | "new" | "sale";

interface RecommendationsRailProps {
  title: string;
  variant?: RecommendationsRailVariant;
  loading?: boolean;
  viewAllHref?: string;
  children?: ReactNode;
}

const COLLAPSED_SKELETON_COUNT = 5;
const EXPANDED_SKELETON_COUNT = 15;

const VARIANT_CONFIG: Record<
  RecommendationsRailVariant,
  { icon: ComponentType<IconProps>; eyebrow: string }
> = {
  personal: { icon: UserIcon, eyebrow: "Персонально для вас" },
  popular: { icon: StarIcon, eyebrow: "Обирають найчастіше" },
  new: { icon: PlusIcon, eyebrow: "Щойно з’явилось" },
  sale: { icon: PercentIcon, eyebrow: "Обмежена пропозиція" },
};

export function RecommendationsRail({
  title,
  variant = "popular",
  loading = false,
  children,
}: RecommendationsRailProps) {
  const [expanded, setExpanded] = useState(false);

  if (!loading && !children) {
    return null;
  }

  const itemCount = loading ? EXPANDED_SKELETON_COUNT : Children.count(children);
  const showToggle = !loading && itemCount > 0;
  const skeletonCount = expanded ? EXPANDED_SKELETON_COUNT : COLLAPSED_SKELETON_COUNT;

  const scrollerClassName = [styles.scroller, !expanded ? styles.scrollerCollapsed : ""]
    .filter(Boolean)
    .join(" ");

  const { icon: VariantIcon, eyebrow } = VARIANT_CONFIG[variant];

  return (
    <section className={styles.section} data-variant={variant} aria-label={title}>
      <div className={styles.headerRow}>
        <div className={styles.titleGroup}>
          <span className={styles.iconBadge} aria-hidden="true">
            <VariantIcon className={iconStyles.icon} />
          </span>
          <div className={styles.titleTextGroup}>
            <p className={styles.eyebrow}>{eyebrow}</p>
            <h2 className={styles.title}>{title}</h2>
          </div>
        </div>
      </div>

      <div className={scrollerClassName} role="list">
        {loading
          ? Array.from({ length: skeletonCount }, (_, index) => (
              <div key={index} className={styles.cardLink} role="listitem" aria-hidden="true">
                <ProductCardSkeleton className={styles.skeletonCard} />
              </div>
            ))
          : children}
      </div>

      {showToggle ? (
        <div className={styles.toggleWrap}>
          <Button
            variant="secondary"
            size="lg"
            onClick={() => setExpanded((value) => !value)}
            aria-expanded={expanded}
            leadingIcon={
              <ChevronDownIcon
                className={`${iconStyles.icon} ${expanded ? styles.toggleIconExpanded : ""}`.trim()}
              />
            }
          >
            {expanded ? "Згорнути" : "Показати ще"}
          </Button>
        </div>
      ) : null}
    </section>
  );
}

import { StarIcon } from "@/shared/ui";
import styles from "./StarRating.module.css";

interface StarRatingProps {
  value: number;
  size?: "sm" | "md";
  className?: string;
}

const STAR_COUNT = 5;

export function StarRating({ value, size = "sm", className }: StarRatingProps) {
  const roundedValue = Math.round(value);

  return (
    <span
      className={[styles.rating, styles[size], className].filter(Boolean).join(" ")}
      role="img"
      aria-label={`Оцінка ${roundedValue} з ${STAR_COUNT}`}
    >
      {Array.from({ length: STAR_COUNT }, (_, index) => (
        <StarIcon
          key={index}
          className={index < roundedValue ? styles.starFilled : styles.starEmpty}
        />
      ))}
    </span>
  );
}

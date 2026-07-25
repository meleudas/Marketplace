"use client";

import { useState } from "react";
import { StarIcon } from "@/shared/ui";
import styles from "./StarRating.module.css";

interface StarRatingProps {
  value: number;
  size?: "sm" | "md";
  className?: string;
  interactive?: boolean;
  onChange?: (value: number) => void;
}

const STAR_COUNT = 5;

export function StarRating({ value, size = "sm", className, interactive, onChange }: StarRatingProps) {
  const [hovered, setHovered] = useState<number | null>(null);
  const roundedValue = Math.round(value);
  const displayValue = interactive && hovered !== null ? hovered : roundedValue;

  if (interactive) {
    return (
      <span
        className={[styles.rating, styles[size], className].filter(Boolean).join(" ")}
        role="radiogroup"
        aria-label="Оцінка"
      >
        {Array.from({ length: STAR_COUNT }, (_, index) => {
          const starValue = index + 1;
          return (
            <button
              key={index}
              type="button"
              className={`${styles.starInteractive} ${starValue <= displayValue ? styles.starFilled : styles.starEmpty}`}
              onClick={() => onChange?.(starValue)}
              onMouseEnter={() => setHovered(starValue)}
              onMouseLeave={() => setHovered(null)}
              aria-label={`${starValue} з ${STAR_COUNT}`}
              role="radio"
              aria-checked={starValue === roundedValue}
            >
              <StarIcon />
            </button>
          );
        })}
      </span>
    );
  }

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

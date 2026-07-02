import type { ButtonHTMLAttributes } from "react";
import { MinusIcon, PlusIcon } from "../icons";
import iconStyles from "../icons/Icon.module.css";
import styles from "./QuantityStepper.module.css";

export interface QuantityStepperProps extends Omit<ButtonHTMLAttributes<HTMLDivElement>, "onChange"> {
  value?: number;
  min?: number;
  max?: number;
  onChange?: (value: number) => void;
  className?: string;
}

export function QuantityStepper({
  value = 1,
  min = 1,
  max = 99,
  onChange,
  className,
  ...props
}: QuantityStepperProps) {
  const decrease = () => {
    if (value > min) {
      onChange?.(value - 1);
    }
  };

  const increase = () => {
    if (value < max) {
      onChange?.(value + 1);
    }
  };

  const classes = [styles.stepper, className].filter(Boolean).join(" ");

  return (
    <div className={classes} {...props}>
      <button
        type="button"
        className={styles.control}
        onClick={decrease}
        disabled={value <= min}
        aria-label="Зменшити кількість"
      >
        <MinusIcon className={iconStyles.icon} />
      </button>
      <span className={styles.value} aria-live="polite">
        {value}
      </span>
      <button
        type="button"
        className={styles.control}
        onClick={increase}
        disabled={value >= max}
        aria-label="Збільшити кількість"
      >
        <PlusIcon className={iconStyles.icon} />
      </button>
    </div>
  );
}

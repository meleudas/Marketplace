"use client";

import {
  createContext,
  forwardRef,
  useContext,
  useState,
  type InputHTMLAttributes,
  type ReactNode,
} from "react";
import { RecordActiveIcon, RecordIcon } from "../icons";
import styles from "./Radio.module.css";

interface RadioGroupContextValue {
  name: string;
  value: string;
  onSelect: (value: string) => void;
  disabled?: boolean;
}

const RadioGroupContext = createContext<RadioGroupContextValue | null>(null);

export interface RadioGroupProps {
  name: string;
  value?: string;
  defaultValue?: string;
  onValueChange?: (value: string) => void;
  label?: ReactNode;
  disabled?: boolean;
  className?: string;
  children: ReactNode;
}

export function RadioGroup({
  name,
  value,
  defaultValue = "",
  onValueChange,
  label,
  disabled,
  className,
  children,
}: RadioGroupProps) {
  const [internalValue, setInternalValue] = useState(defaultValue);
  const currentValue = value ?? internalValue;

  const handleSelect = (nextValue: string) => {
    if (value === undefined) {
      setInternalValue(nextValue);
    }
    onValueChange?.(nextValue);
  };

  return (
    <RadioGroupContext.Provider
      value={{
        name,
        value: currentValue,
        onSelect: handleSelect,
        disabled,
      }}
    >
      <div
        className={[styles.group, className ?? ""].filter(Boolean).join(" ")}
        role="radiogroup"
        aria-label={typeof label === "string" ? label : undefined}
      >
        {label ? <p className={styles.groupLabel}>{label}</p> : null}
        <div className={styles.options}>{children}</div>
      </div>
    </RadioGroupContext.Provider>
  );
}

export interface RadioProps extends Omit<InputHTMLAttributes<HTMLInputElement>, "type" | "name"> {
  value: string;
  label?: ReactNode;
}

export const Radio = forwardRef<HTMLInputElement, RadioProps>(function Radio(
  { value, label, className, disabled, id, onChange, ...props },
  ref,
) {
  const group = useContext(RadioGroupContext);

  if (!group) {
    throw new Error("Radio must be used within RadioGroup");
  }

  const inputId = id ?? `${group.name}-${value}`;
  const isDisabled = disabled ?? group.disabled;
  const checked = group.value === value;

  return (
    <label
      className={[styles.root, isDisabled ? styles.disabled : "", className ?? ""].filter(Boolean).join(" ")}
    >
      <input
        ref={ref}
        id={inputId}
        type="radio"
        name={group.name}
        value={value}
        className={styles.input}
        checked={checked}
        disabled={isDisabled}
        onChange={(event) => {
          onChange?.(event);
          group.onSelect(value);
        }}
        {...props}
      />

      <span className={styles.control}>
        <RecordIcon className={styles.uncheckedIcon} />
        <RecordActiveIcon className={styles.checkedIcon} />
      </span>

      {label ? <span className={styles.label}>{label}</span> : null}
    </label>
  );
});

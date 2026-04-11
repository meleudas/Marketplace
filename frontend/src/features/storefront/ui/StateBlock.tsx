import styles from "./StateBlock.module.css";

interface StateBlockProps {
  message: string;
  isError?: boolean;
}

export function StateBlock({ message, isError = false }: StateBlockProps) {
  return <div className={`${styles.state} ${isError ? styles.error : ""}`.trim()}>{message}</div>;
}


import { WORKSPACE_COMPANY_ID } from "@/features/workspace/config/workspace.constants";
import styles from "./WorkspaceScreen.module.css";

interface WorkspacePlaceholderScreenProps {
  title: string;
  description: string;
}

export function WorkspacePlaceholderScreen({ title, description }: WorkspacePlaceholderScreenProps) {
  return (
    <section className={styles.card}>
      <h2 className={styles.sectionTitle}>{title}</h2>
      <p className={styles.row}>{description}</p>
      <p className={styles.row}>
        <span className={styles.label}>Company ID:</span> {WORKSPACE_COMPANY_ID}
      </p>
    </section>
  );
}


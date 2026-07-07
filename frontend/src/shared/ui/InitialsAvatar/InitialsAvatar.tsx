import styles from "./InitialsAvatar.module.css";

interface InitialsAvatarProps {
  firstName: string;
  lastName: string;
  className?: string;
}

export function InitialsAvatar({ firstName, lastName, className }: InitialsAvatarProps) {
  const f = (firstName ?? "").trim().charAt(0).toUpperCase();
  const l = (lastName ?? "").trim().charAt(0).toUpperCase();
  const initials = `${f}${l}` || "?";

  return (
    <div className={`${styles.avatar} ${className ?? ""}`}>
      <span className={styles.initial}>{initials}</span>
    </div>
  );
}

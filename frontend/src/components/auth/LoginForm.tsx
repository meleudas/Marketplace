"use client";

import { FormEvent, useState } from "react";
import { useAuth } from "@/hooks/useAuth";
import styles from "./LoginForm.module.css";

export function LoginForm() {
  const login = useAuth((state) => state.login);
  const loading = useAuth((state) => state.loading);

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setSuccess(null);

    const result = await login({ email: email.trim(), password });

    if (!result.success) {
      setError(result.message);
      return;
    }

    setSuccess(result.message);
    setPassword("");
  };

  return (
    <form onSubmit={handleSubmit} className={styles.form}>
      <h2 className={styles.title}>Login</h2>

      <div className={styles.field}>
        <label className={styles.label} htmlFor="login-email">
          Email
        </label>
        <input
          id="login-email"
          type="email"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          required
          className={styles.input}
          placeholder="you@example.com"
        />
      </div>

      <div className={styles.field}>
        <label className={styles.label} htmlFor="login-password">
          Password
        </label>
        <input
          id="login-password"
          type="password"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          required
          className={styles.input}
          placeholder="********"
        />
      </div>

      {error ? <p className={styles.errorMessage}>{error}</p> : null}
      {success ? <p className={styles.successMessage}>{success}</p> : null}

      <button
        type="submit"
        disabled={loading}
        className={styles.submitButton}
      >
        {loading ? "Logging in..." : "Login"}
      </button>
    </form>
  );
}


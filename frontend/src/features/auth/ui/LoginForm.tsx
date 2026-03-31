"use client";

import { FormEvent, useState } from "react";
import { useAuth } from "@/features/auth/model/auth.store";
import styles from "./LoginForm.module.css";

interface LoginFormProps {
  onSwitchToRegister: () => void;
  onForgotPassword: () => void;
}

export function LoginForm({ onSwitchToRegister, onForgotPassword }: LoginFormProps) {
  const login = useAuth((state) => state.login);
  const startGoogleLogin = useAuth((state) => state.startGoogleLogin);
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

      <button
        type="button"
        disabled={loading}
        onClick={startGoogleLogin}
        className={styles.googleButton}
      >
        Continue with Google
      </button>

      <div className={styles.secondaryActions}>
        <button type="button" onClick={onForgotPassword} className={styles.linkButton}>
          Forgot password?
        </button>
        <p className={styles.switchText}>
          No account yet?{" "}
          <button type="button" onClick={onSwitchToRegister} className={styles.linkButton}>
            Register
          </button>
        </p>
      </div>
    </form>
  );
}




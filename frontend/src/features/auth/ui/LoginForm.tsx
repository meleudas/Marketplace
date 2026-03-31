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
  const [rememberMe, setRememberMe] = useState(false);
  const [twoFactorCode, setTwoFactorCode] = useState("");
  const [isTwoFactorStep, setIsTwoFactorStep] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setSuccess(null);

    const result = await login({
      email: email.trim(),
      password,
      rememberMe,
      twoFactorCode: isTwoFactorStep ? twoFactorCode.trim() : null,
    });

    if (!result.success) {
      if (result.requiresTwoFactor) {
        setIsTwoFactorStep(true);
        setSuccess(result.message);
        return;
      }

      setError(result.message);
      return;
    }

    setSuccess(result.message);
    setIsTwoFactorStep(false);
    setTwoFactorCode("");
    setPassword("");
  };

  const handleBackToCredentials = () => {
    setIsTwoFactorStep(false);
    setTwoFactorCode("");
    setError(null);
    setSuccess(null);
  };

  return (
    <form onSubmit={handleSubmit} className={styles.form}>
      <h2 className={styles.title}>Login</h2>

      {!isTwoFactorStep ? (
        <>
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

          <label className={styles.checkboxRow} htmlFor="login-remember-me">
            <input
              id="login-remember-me"
              type="checkbox"
              checked={rememberMe}
              onChange={(event) => setRememberMe(event.target.checked)}
              className={styles.checkbox}
            />
            Remember me
          </label>
        </>
      ) : (
        <>
          <p className={styles.helperText}>
            Enter the verification code sent to your email.
          </p>
          <div className={styles.field}>
            <label className={styles.label} htmlFor="login-two-factor-code">
              2FA Code
            </label>
            <input
              id="login-two-factor-code"
              type="text"
              value={twoFactorCode}
              onChange={(event) => setTwoFactorCode(event.target.value)}
              required
              className={styles.input}
              placeholder="123456"
            />
          </div>
        </>
      )}

      {error ? <p className={styles.errorMessage}>{error}</p> : null}
      {success ? <p className={styles.successMessage}>{success}</p> : null}

      <button
        type="submit"
        disabled={loading}
        className={styles.submitButton}
      >
        {loading ? "Logging in..." : isTwoFactorStep ? "Verify and login" : "Login"}
      </button>

      {isTwoFactorStep ? (
        <button
          type="button"
          disabled={loading}
          onClick={handleBackToCredentials}
          className={styles.secondaryButton}
        >
          Use different account
        </button>
      ) : null}

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




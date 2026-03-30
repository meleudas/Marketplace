"use client";

import { FormEvent, useEffect, useState } from "react";
import { useAuth } from "@/hooks/useAuth";
import styles from "./ForgotPasswordForm.module.css";

const RESEND_COOLDOWN_SECONDS = 30;

interface ForgotPasswordFormProps {
  onSwitchToLogin: () => void;
}

export function ForgotPasswordForm({ onSwitchToLogin }: ForgotPasswordFormProps) {
  const forgotPassword = useAuth((state) => state.forgotPassword);
  const resetPassword = useAuth((state) => state.resetPassword);
  const loading = useAuth((state) => state.loading);

  const [step, setStep] = useState<1 | 2>(1);
  const [email, setEmail] = useState("");
  const [token, setToken] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [resendSecondsLeft, setResendSecondsLeft] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    if (resendSecondsLeft <= 0) {
      return;
    }

    const timerId = window.setInterval(() => {
      setResendSecondsLeft((current) => (current > 0 ? current - 1 : 0));
    }, 1000);

    return () => {
      window.clearInterval(timerId);
    };
  }, [resendSecondsLeft]);

  const handleSendCode = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setSuccess(null);

    const result = await forgotPassword({ email: email.trim() });

    if (!result.success) {
      setError(result.message);
      return;
    }

    setSuccess(result.message);
    setStep(2);
    setResendSecondsLeft(RESEND_COOLDOWN_SECONDS);
  };

  const handleResendCode = async () => {
    if (resendSecondsLeft > 0) {
      return;
    }

    setError(null);
    setSuccess(null);

    const result = await forgotPassword({ email: email.trim() });
    if (!result.success) {
      setError(result.message);
      return;
    }

    setSuccess("Reset token sent again. Check your email.");
    setResendSecondsLeft(RESEND_COOLDOWN_SECONDS);
  };

  const handleResetPassword = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setSuccess(null);

    const result = await resetPassword({
      email: email.trim(),
      token: token.trim(),
      newPassword,
    });

    if (!result.success) {
      setError(result.message);
      return;
    }

    setSuccess(result.message);
    setToken("");
    setNewPassword("");
  };

  return (
    <form onSubmit={step === 1 ? handleSendCode : handleResetPassword} className={styles.form}>
      <h2 className={styles.title}>Reset password</h2>
      <p className={styles.description}>
        {step === 1
          ? "Enter your email and we will send you a reset token."
          : "Enter the token from email and set your new password."}
      </p>

      <div className={styles.field}>
        <label className={styles.label} htmlFor="forgot-email">
          Email
        </label>
        <input
          id="forgot-email"
          type="email"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          required
          readOnly={step === 2}
          className={styles.input}
          placeholder="you@example.com"
        />
      </div>

      {step === 2 ? (
        <>
          <div className={styles.field}>
            <label className={styles.label} htmlFor="forgot-token">
              Reset token
            </label>
            <input
              id="forgot-token"
              type="text"
              value={token}
              onChange={(event) => setToken(event.target.value)}
              required
              className={styles.input}
              placeholder="Paste token from email"
            />
          </div>

          <div className={styles.field}>
            <label className={styles.label} htmlFor="forgot-new-password">
              New password
            </label>
            <input
              id="forgot-new-password"
              type="password"
              value={newPassword}
              onChange={(event) => setNewPassword(event.target.value)}
              required
              className={styles.input}
              placeholder="********"
            />
          </div>
        </>
      ) : null}

      {error ? <p className={styles.errorMessage}>{error}</p> : null}
      {success ? <p className={styles.successMessage}>{success}</p> : null}

      <button type="submit" disabled={loading} className={styles.submitButton}>
        {step === 1
          ? loading
            ? "Sending..."
            : "Send reset token"
          : loading
            ? "Resetting..."
            : "Reset password"}
      </button>

      <div className={styles.secondaryActions}>
        {step === 2 ? (
          <>
            <button
              type="button"
              onClick={() => {
                void handleResendCode();
              }}
              disabled={loading || resendSecondsLeft > 0}
              className={styles.linkButton}
            >
              Did not receive the code? Send again
            </button>
            {resendSecondsLeft > 0 ? (
              <p className={styles.timerText}>Available in {resendSecondsLeft}s</p>
            ) : null}
          </>
        ) : null}

        <button type="button" onClick={onSwitchToLogin} className={styles.linkButton}>
          Back to login
        </button>
      </div>
    </form>
  );
}


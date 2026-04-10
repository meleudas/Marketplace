"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import {
  forgotPasswordFormSchema,
  forgotPasswordResetSchema,
  type ForgotPasswordFormValues,
} from "@/features/auth/model/auth.form-schemas";
import { useAuth } from "@/features/auth/model/auth.store";
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
  const [resendSecondsLeft, setResendSecondsLeft] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const {
    register: registerField,
    handleSubmit,
    getValues,
    setError: setFieldError,
    clearErrors,
    resetField,
    formState: { errors },
  } = useForm<ForgotPasswordFormValues>({
    resolver: zodResolver(forgotPasswordFormSchema),
    defaultValues: {
      email: "",
      token: "",
      newPassword: "",
    },
  });

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

  const handleSendCode = async (values: ForgotPasswordFormValues) => {
    setError(null);
    setSuccess(null);

    const result = await forgotPassword({ email: values.email.trim() });

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

    const email = getValues("email").trim();
    const result = await forgotPassword({ email });
    if (!result.success) {
      setError(result.message);
      return;
    }

    setSuccess("Reset token sent again. Check your email.");
    setResendSecondsLeft(RESEND_COOLDOWN_SECONDS);
  };

  const handleResetPassword = async (values: ForgotPasswordFormValues) => {
    const parsed = forgotPasswordResetSchema.safeParse(values);
    if (!parsed.success) {
      for (const issue of parsed.error.issues) {
        const fieldName = issue.path[0];
        if (fieldName === "email") {
          setFieldError("email", { type: "manual", message: issue.message });
        }
        if (fieldName === "token") {
          setFieldError("token", { type: "manual", message: issue.message });
        }
        if (fieldName === "newPassword") {
          setFieldError("newPassword", { type: "manual", message: issue.message });
        }
      }
      return;
    }

    setError(null);
    setSuccess(null);

    const result = await resetPassword({
      email: parsed.data.email,
      token: parsed.data.token,
      newPassword: parsed.data.newPassword,
    });

    if (!result.success) {
      setError(result.message);
      return;
    }

    setSuccess(result.message);
    resetField("token");
    resetField("newPassword");
  };

  const onSubmit = async (values: ForgotPasswordFormValues) => {
    if (step === 1) {
      clearErrors(["token", "newPassword"]);
      await handleSendCode(values);
      return;
    }

    await handleResetPassword(values);
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className={styles.form}>
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
          {...registerField("email")}
          readOnly={step === 2}
          className={styles.input}
          placeholder="you@example.com"
        />
        {errors.email ? <p className={styles.fieldError}>{errors.email.message}</p> : null}
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
              {...registerField("token")}
              className={styles.input}
              placeholder="Paste token from email"
            />
            {errors.token ? <p className={styles.fieldError}>{errors.token.message}</p> : null}
          </div>

          <div className={styles.field}>
            <label className={styles.label} htmlFor="forgot-new-password">
              New password
            </label>
            <input
              id="forgot-new-password"
              type="password"
              {...registerField("newPassword")}
              className={styles.input}
              placeholder="********"
            />
            {errors.newPassword ? (
              <p className={styles.fieldError}>{errors.newPassword.message}</p>
            ) : null}
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




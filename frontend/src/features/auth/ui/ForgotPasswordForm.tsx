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
import { Button, TextField } from "@/shared/ui";
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

    setSuccess("Код скидання надіслано ще раз. Перевірте пошту.");
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
      <div className={styles.header}>
        <h2 className={styles.title}>Відновлення пароля</h2>
        <p className={styles.description}>
          {step === 1
            ? "Введіть електронну пошту, і ми надішлемо код скидання."
            : "Використайте код з листа, щоб задати новий пароль."}
        </p>
      </div>

      <TextField
        id="forgot-email"
        label="Ел. пошта"
        kind="email"
        placeholder="ваша@пошта.укр"
        autoComplete="email"
        readOnly={step === 2}
        disabled={loading}
        error={errors.email?.message}
        {...registerField("email")}
      />

      {step === 2 ? (
        <>
          <TextField
            id="forgot-token"
            label="Код скидання"
            kind="text"
            placeholder="Вставте код з листа"
            autoComplete="one-time-code"
            disabled={loading}
            error={errors.token?.message}
            {...registerField("token")}
          />

          <TextField
            id="forgot-new-password"
            label="Новий пароль"
            kind="password"
            placeholder="••••••••"
            autoComplete="new-password"
            disabled={loading}
            error={errors.newPassword?.message}
            {...registerField("newPassword")}
          />
        </>
      ) : null}

      {error ? (
        <div className={styles.errorMessage} role="alert">
          {error}
        </div>
      ) : null}
      {success ? <div className={styles.successMessage}>{success}</div> : null}

      <Button
        type="submit"
        variant="gradient"
        fullWidth
        disabled={loading}
        className={styles.submitButton}
      >
        {step === 1
          ? loading
            ? "Надсилаємо код..."
            : "Надіслати код скидання"
          : loading
            ? "Оновлюємо пароль..."
            : "Оновити пароль"}
      </Button>

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
              Не отримали код? Надіслати ще раз
            </button>
            {resendSecondsLeft > 0 ? (
              <p className={styles.timerText}>Доступно через {resendSecondsLeft} с</p>
            ) : null}
          </>
        ) : null}

        <button type="button" onClick={onSwitchToLogin} className={styles.linkButton}>
          Повернутися до входу
        </button>
      </div>
    </form>
  );
}




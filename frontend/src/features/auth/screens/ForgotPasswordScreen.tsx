"use client";

import Link from "next/link";
import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import {
  forgotPasswordFormSchema,
  forgotPasswordResetSchema,
  type ForgotPasswordFormValues,
} from "@/features/auth/model/auth.form-schemas";
import { useAuth } from "@/features/auth/model/auth.store";
import { Button, PageLayout, SideDecorShell, TextField } from "@/shared/ui";
import styles from "./ForgotPasswordScreen.module.css";

const RESEND_COOLDOWN_SECONDS = 30;

export function ForgotPasswordScreen() {
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
    defaultValues: { email: "", token: "", newPassword: "" },
  });

  useEffect(() => {
    if (resendSecondsLeft <= 0) return;
    const id = window.setInterval(() => {
      setResendSecondsLeft((c) => (c > 0 ? c - 1 : 0));
    }, 1000);
    return () => window.clearInterval(id);
  }, [resendSecondsLeft]);

  const handleSendCode = async (values: ForgotPasswordFormValues) => {
    setError(null);
    setSuccess(null);
    const result = await forgotPassword({ email: values.email.trim() });
    if (!result.success) { setError(result.message); return; }
    setSuccess(result.message);
    setStep(2);
    setResendSecondsLeft(RESEND_COOLDOWN_SECONDS);
  };

  const handleResendCode = async () => {
    if (resendSecondsLeft > 0) return;
    setError(null);
    setSuccess(null);
    const email = getValues("email").trim();
    const result = await forgotPassword({ email });
    if (!result.success) { setError(result.message); return; }
    setSuccess("Код скидання надіслано ще раз. Перевірте пошту.");
    setResendSecondsLeft(RESEND_COOLDOWN_SECONDS);
  };

  const mapResetError = (message: string): string => {
    const lower = message.toLowerCase();
    if (lower.includes("invalid") && lower.includes("token")) {
      return "Код скидання невідповідний або застарілий. Запросіть новий код.";
    }
    return message;
  };

  const handleResetPassword = async (values: ForgotPasswordFormValues) => {
    const parsed = forgotPasswordResetSchema.safeParse(values);
    if (!parsed.success) {
      for (const issue of parsed.error.issues) {
        const f = issue.path[0];
        if (f === "email") setFieldError("email", { type: "manual", message: issue.message });
        if (f === "token") setFieldError("token", { type: "manual", message: issue.message });
        if (f === "newPassword") setFieldError("newPassword", { type: "manual", message: issue.message });
      }
      return;
    }
    setError(null);
    setSuccess(null);
    const result = await resetPassword({ email: parsed.data.email, token: parsed.data.token, newPassword: parsed.data.newPassword });
    if (!result.success) { setError(mapResetError(result.message)); return; }
    setSuccess(result.message);
    resetField("token");
    resetField("newPassword");
  };

  const onSubmit = async (values: ForgotPasswordFormValues) => {
    if (step === 1) { clearErrors(["token", "newPassword"]); await handleSendCode(values); return; }
    await handleResetPassword(values);
  };

  return (
    <PageLayout footerProps={{ homeHref: "/" }}>
      <SideDecorShell contentClassName={styles.center}>
        <div className={styles.card}>
          <div className={styles.header}>
            <h1 className={styles.title}>Відновлення пароля</h1>
            <p className={styles.description}>
              {step === 1
                ? "Введіть електронну пошту, і ми надішлемо код скидання."
                : "Використайте код з листа, щоб задати новий пароль."}
            </p>
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className={styles.form} noValidate>
            <TextField
              id="forgot-email"
              label="Ел. пошта"
              kind="email"
              placeholder="account@booktop.ua"
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

            {error ? <div className={styles.error} role="alert">{error}</div> : null}
            {success ? <div className={styles.success}>{success}</div> : null}

            <div className={styles.actions}>
              <Button type="submit" variant="primary" fullWidth disabled={loading}>
                {step === 1
                  ? loading ? "Надсилаємо код..." : "Надіслати код скидання"
                  : loading ? "Оновлюємо пароль..." : "Оновити пароль"}
              </Button>
            </div>
          </form>

          <div className={styles.secondaryActions}>
            {step === 2 ? (
              <>
                <button
                  type="button"
                  onClick={() => { void handleResendCode(); }}
                  disabled={loading || resendSecondsLeft > 0}
                  className={styles.linkBtn}
                >
                  Не отримали код? Надіслати ще раз
                </button>
                {resendSecondsLeft > 0 ? (
                  <p className={styles.timerText}>Доступно через {resendSecondsLeft} с</p>
                ) : null}
              </>
            ) : null}
            <Link href="/auth/login" className={styles.linkBtn}>Повернутися до входу</Link>
          </div>

          </div>
      </SideDecorShell>
    </PageLayout>
  );
}

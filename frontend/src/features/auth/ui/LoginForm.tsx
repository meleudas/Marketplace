"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { loginFormSchema, type LoginFormValues } from "@/features/auth/model/auth.form-schemas";
import { useAuth } from "@/features/auth/model/auth.store";
import { Button, TextField } from "@/shared/ui";
import styles from "./LoginForm.module.css";

interface LoginFormProps {
  onSwitchToRegister: () => void;
  onForgotPassword: () => void;
}

export function LoginForm({ onSwitchToRegister, onForgotPassword }: LoginFormProps) {
  const login = useAuth((state) => state.login);
  const startGoogleLogin = useAuth((state) => state.startGoogleLogin);
  const loading = useAuth((state) => state.loading);

  const [isTwoFactorStep, setIsTwoFactorStep] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const {
    register: registerField,
    handleSubmit,
    formState: { errors },
    setError: setFieldError,
    clearErrors,
    resetField,
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginFormSchema),
    defaultValues: {
      email: "",
      password: "",
      twoFactorCode: "",
    },
  });

  const onSubmit = async (values: LoginFormValues) => {
    setError(null);
    setSuccess(null);

    const twoFactorCode = values.twoFactorCode?.trim() ?? "";
    if (isTwoFactorStep && !twoFactorCode) {
      setFieldError("twoFactorCode", {
        type: "manual",
        message: "Потрібен код підтвердження",
      });
      return;
    }

    const result = await login({
      email: values.email.trim(),
      password: values.password,
      twoFactorCode: isTwoFactorStep ? twoFactorCode : null,
    });

    if (!result.success) {
      if (result.requiresTwoFactor) {
        setIsTwoFactorStep(true);
        clearErrors("twoFactorCode");
        setSuccess(result.message);
        return;
      }

      setError(result.message);
      return;
    }

    setSuccess(result.message);
    setIsTwoFactorStep(false);
    resetField("twoFactorCode");
    resetField("password");
  };

  const handleBackToCredentials = () => {
    setIsTwoFactorStep(false);
    resetField("twoFactorCode");
    clearErrors("twoFactorCode");
    setError(null);
    setSuccess(null);
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className={styles.form}>
      <div className={styles.header}>
        <h2 className={styles.title}>{isTwoFactorStep ? "Підтвердіть вхід" : "Ласкаво просимо назад"}</h2>
        <p className={styles.description}>
          {isTwoFactorStep
            ? "Введіть код підтвердження з пошти, щоб продовжити."
            : "Увійдіть за допомогою електронної пошти й пароля або продовжіть через Google."}
        </p>
      </div>

      {!isTwoFactorStep ? (
        <>
          <TextField
            id="login-email"
            label="Ел. пошта"
            kind="email"
            placeholder="ваша@пошта.укр"
            autoComplete="email"
            disabled={loading}
            error={errors.email?.message}
            {...registerField("email")}
          />

          <TextField
            id="login-password"
            label="Пароль"
            kind="password"
            placeholder="••••••••"
            autoComplete="current-password"
            disabled={loading}
            error={errors.password?.message}
            {...registerField("password")}
          />
        </>
      ) : (
        <>
          <TextField
            id="login-two-factor-code"
            label="Код підтвердження"
            kind="text"
            placeholder="123456"
            autoComplete="one-time-code"
            disabled={loading}
            error={errors.twoFactorCode?.message}
            {...registerField("twoFactorCode")}
          />
        </>
      )}

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
        {loading ? "Вхід..." : isTwoFactorStep ? "Підтвердити і увійти" : "Увійти"}
      </Button>

      {isTwoFactorStep ? (
        <Button
          type="button"
          variant="dark"
          fullWidth
          disabled={loading}
          onClick={handleBackToCredentials}
          className={styles.secondaryButton}
        >
          Використати інший акаунт
        </Button>
      ) : null}

      <Button
        type="button"
        variant="secondary"
        fullWidth
        disabled={loading}
        onClick={startGoogleLogin}
        className={styles.googleButton}
      >
        Продовжити через Google
      </Button>

      <div className={styles.secondaryActions}>
        <button type="button" onClick={onForgotPassword} className={styles.linkButton}>
          Забули пароль?
        </button>
        <p className={styles.switchText}>
          Немає акаунта?{" "}
          <button type="button" onClick={onSwitchToRegister} className={styles.linkButton}>
            Створити
          </button>
        </p>
      </div>
    </form>
  );
}




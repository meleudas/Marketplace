"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { loginFormSchema, type LoginFormValues } from "@/features/auth/model/auth.form-schemas";
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
        message: "2FA code is required",
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
              {...registerField("email")}
              className={styles.input}
              placeholder="you@example.com"
            />
            {errors.email ? <p className={styles.fieldError}>{errors.email.message}</p> : null}
          </div>

          <div className={styles.field}>
            <label className={styles.label} htmlFor="login-password">
              Password
            </label>
            <input
              id="login-password"
              type="password"
              {...registerField("password")}
              className={styles.input}
              placeholder="********"
            />
            {errors.password ? <p className={styles.fieldError}>{errors.password.message}</p> : null}
          </div>
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
              {...registerField("twoFactorCode")}
              className={styles.input}
              placeholder="123456"
            />
            {errors.twoFactorCode ? (
              <p className={styles.fieldError}>{errors.twoFactorCode.message}</p>
            ) : null}
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




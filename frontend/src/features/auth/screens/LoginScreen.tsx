"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { loginFormSchema, type LoginFormValues } from "@/features/auth/model/auth.form-schemas";
import { useAuth } from "@/features/auth/model/auth.store";
import { Button, PageLayout, SideDecorShell, TextField } from "@/shared/ui";
import { GoogleIcon } from "@/shared/ui/icons";
import styles from "./LoginScreen.module.css";

function resolvePostLoginPath(redirectTarget: string | null): string {
  if (redirectTarget?.startsWith("/") && !redirectTarget.startsWith("//")) {
    return redirectTarget;
  }

  return "/";
}

export function LoginScreen() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const login = useAuth((state) => state.login);
  const startGoogleLogin = useAuth((state) => state.startGoogleLogin);
  const loading = useAuth((state) => state.loading);

  const [isTwoFactorStep, setIsTwoFactorStep] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [googleLoading, setGoogleLoading] = useState(false);

  // reset google spinner if user navigates back without completing auth
  useEffect(() => {
    const resetIfPersisted = (e: PageTransitionEvent) => {
      if (e.persisted) setGoogleLoading(false);
    };
    window.addEventListener("pageshow", resetIfPersisted);
    return () => window.removeEventListener("pageshow", resetIfPersisted);
  }, []);

  const {
    register: registerField,
    handleSubmit,
    formState: { errors },
    setError: setFieldError,
    clearErrors,
    resetField,
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginFormSchema),
    defaultValues: { email: "", password: "", twoFactorCode: "" },
  });

  const onSubmit = async (values: LoginFormValues) => {
    setError(null);
    setSuccess(null);

    const twoFactorCode = values.twoFactorCode?.trim() ?? "";
    if (isTwoFactorStep && !twoFactorCode) {
      setFieldError("twoFactorCode", { type: "manual", message: "Потрібен код підтвердження" });
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

    router.replace(resolvePostLoginPath(searchParams.get("redirect")));
  };

  const handleBackToCredentials = () => {
    setIsTwoFactorStep(false);
    resetField("twoFactorCode");
    clearErrors("twoFactorCode");
    setError(null);
    setSuccess(null);
  };

  return (
    <PageLayout footerProps={{ homeHref: "/" }}>
      <SideDecorShell contentClassName={styles.center}>
        <div className={styles.card}>
          <div className={styles.header}>
            <h1 className={styles.title}>{isTwoFactorStep ? "Підтвердіть вхід" : "Вхід"}</h1>
            <p className={styles.description}>
              {isTwoFactorStep
                ? "Введіть код підтвердження з пошти, щоб продовжити."
                : "Увійдіть за допомогою електронної пошти та пароля."}
            </p>
          </div>

          <form
            onSubmit={(event) => {
              event.preventDefault();
              void handleSubmit(onSubmit)(event);
            }}
            className={styles.form}
            noValidate
          >
            {!isTwoFactorStep ? (
              <>
                <TextField
                  id="login-email"
                  label="Ел. пошта"
                  kind="email"
                  placeholder="account@booktop.ua"
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
            )}

            {error ? <div className={styles.error} role="alert">{error}</div> : null}
            {success ? <div className={styles.success}>{success}</div> : null}

            <div className={styles.actions}>
              <Button type="submit" variant="primary" fullWidth disabled={loading || googleLoading}>
                {loading ? "Вхід..." : isTwoFactorStep ? "Підтвердити і увійти" : "Увійти"}
              </Button>

              {isTwoFactorStep ? (
                <Button type="button" variant="dark" fullWidth disabled={loading || googleLoading} onClick={handleBackToCredentials}>
                  Використати інший акаунт
                </Button>
              ) : null}
            </div>
          </form>

          <div className={styles.divider}>
            <span className={styles.dividerLine} />
            <span className={styles.dividerText}>або</span>
            <span className={styles.dividerLine} />
          </div>

          <button
            type="button"
            onClick={() => { setGoogleLoading(true); startGoogleLogin(); }}
            disabled={loading || googleLoading}
            className={styles.googleBtn}
          >
            {googleLoading ? (
              <span className={styles.spinner} />
            ) : (
              <GoogleIcon width={20} height={20} />
            )}
            <span>{googleLoading ? "Перенаправляємо..." : "Продовжити через Google"}</span>
          </button>

          <div className={styles.links}>
            <Link href="/auth/forgot-password" className={styles.linkBtn}>Забули пароль?</Link>
            <p className={styles.switchText}>
              Немає акаунта?{" "}
              <Link href="/auth/register" className={styles.linkBtn}>Створити</Link>
            </p>
          </div>

        </div>
      </SideDecorShell>
    </PageLayout>
  );
}

"use client";

import Link from "next/link";
import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { registerFormSchema, type RegisterFormValues } from "@/features/auth/model/auth.form-schemas";
import { useAuth } from "@/features/auth/model/auth.store";
import { Button, PageLayout, SideDecorShell, TextField } from "@/shared/ui";
import { GoogleIcon } from "@/shared/ui/icons";
import styles from "./RegisterScreen.module.css";

export function RegisterScreen() {
  const register = useAuth((state) => state.register);
  const startGoogleLogin = useAuth((state) => state.startGoogleLogin);
  const loading = useAuth((state) => state.loading);

  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [googleLoading, setGoogleLoading] = useState(false);

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
    resetField,
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerFormSchema),
    defaultValues: { userName: "", email: "", phoneNumber: "", password: "" },
  });

  const onSubmit = async (values: RegisterFormValues) => {
    setError(null);
    setSuccess(null);

    const phoneNumber = values.phoneNumber?.trim() ?? "";
    const result = await register({
      email: values.email.trim(),
      password: values.password,
      userName: values.userName.trim(),
      phoneNumber: phoneNumber.length > 0 ? phoneNumber : null,
    });

    if (!result.success) {
      setError(result.message);
      return;
    }

    setSuccess(result.message);
    resetField("password");
  };

  return (
    <PageLayout footerProps={{ homeHref: "/" }}>
      <SideDecorShell contentClassName={styles.center}>
        <div className={styles.card}>
          <div className={styles.header}>
            <h1 className={styles.title}>Створіть акаунт</h1>
            <p className={styles.description}>
              Використайте електронну пошту, ім&apos;я користувача та пароль, щоб почати.
            </p>
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className={styles.form}>
            <TextField
              id="register-userName"
              label="Ім'я користувача"
              kind="text"
              placeholder="іван"
              autoComplete="username"
              disabled={loading}
              error={errors.userName?.message}
              {...registerField("userName")}
            />
            <TextField
              id="register-email"
              label="Ел. пошта"
              kind="email"
              placeholder="account@booktop.ua"
              autoComplete="email"
              disabled={loading}
              error={errors.email?.message}
              {...registerField("email")}
            />
            <TextField
              id="register-phoneNumber"
              label="Номер телефону (Необов'язково)"
              kind="tel"
              placeholder="+380 67 123 45 67"
              autoComplete="tel"
              disabled={loading}
              error={errors.phoneNumber?.message}
              {...registerField("phoneNumber")}
            />
            <TextField
              id="register-password"
              label="Пароль"
              kind="password"
              placeholder="••••••••"
              autoComplete="new-password"
              disabled={loading}
              error={errors.password?.message}
              {...registerField("password")}
            />

            {error ? <div className={styles.error} role="alert">{error}</div> : null}
            {success ? <div className={styles.success}>{success}</div> : null}

            <div className={styles.actions}>
              <Button type="submit" variant="primary" fullWidth disabled={loading || googleLoading}>
                {loading ? "Створюємо акаунт..." : "Створити акаунт"}
              </Button>
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

          <p className={styles.switchText}>
            Уже маєте акаунт?{" "}
            <Link href="/auth/login" className={styles.linkBtn}>Увійти</Link>
          </p>

        </div>
      </SideDecorShell>
    </PageLayout>
  );
}

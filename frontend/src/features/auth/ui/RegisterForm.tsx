"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { registerFormSchema, type RegisterFormValues } from "@/features/auth/model/auth.form-schemas";
import { useAuth } from "@/features/auth/model/auth.store";
import { Button, TextField } from "@/shared/ui";
import styles from "./RegisterForm.module.css";

interface RegisterFormProps {
  onSwitchToLogin: () => void;
}

export function RegisterForm({ onSwitchToLogin }: RegisterFormProps) {
  const register = useAuth((state) => state.register);
  const loading = useAuth((state) => state.loading);

  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const {
    register: registerField,
    handleSubmit,
    formState: { errors },
    resetField,
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerFormSchema),
    defaultValues: {
      userName: "",
      email: "",
      phoneNumber: "",
      password: "",
    },
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
    <form onSubmit={handleSubmit(onSubmit)} className={styles.form}>
      <div className={styles.header}>
        <h2 className={styles.title}>Створіть акаунт</h2>
        <p className={styles.description}>
          Використайте електронну пошту, ім&apos;я користувача та пароль, щоб почати. За бажанням
          можна додати номер телефону.
        </p>
      </div>

      <TextField
        id="register-userName"
        label="Ім&apos;я користувача"
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
        placeholder="ваша@пошта.укр"
        autoComplete="email"
        disabled={loading}
        error={errors.email?.message}
        {...registerField("email")}
      />

      <TextField
        id="register-phoneNumber"
        label="Номер телефону"
        hint="Необов&apos;язково"
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
        {loading ? "Створюємо акаунт..." : "Створити акаунт"}
      </Button>

      <p className={styles.switchText}>
        Уже маєте акаунт?{" "}
        <button type="button" onClick={onSwitchToLogin} className={styles.linkButton}>
          Увійти
        </button>
      </p>
    </form>
  );
}




"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { registerFormSchema, type RegisterFormValues } from "@/features/auth/model/auth.form-schemas";
import { useAuth } from "@/features/auth/model/auth.store";
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
      <h2 className={styles.title}>Register</h2>

      <div className={styles.field}>
        <label className={styles.label} htmlFor="register-userName">
          Username
        </label>
        <input
          id="register-userName"
          type="text"
          {...registerField("userName")}
          className={styles.input}
          placeholder="john"
        />
        {errors.userName ? <p className={styles.fieldError}>{errors.userName.message}</p> : null}
      </div>

      <div className={styles.field}>
        <label className={styles.label} htmlFor="register-email">
          Email
        </label>
        <input
          id="register-email"
          type="email"
          {...registerField("email")}
          className={styles.input}
          placeholder="you@example.com"
        />
        {errors.email ? <p className={styles.fieldError}>{errors.email.message}</p> : null}
      </div>

      <div className={styles.field}>
        <label className={styles.label} htmlFor="register-phoneNumber">
          Phone number (optional)
        </label>
        <input
          id="register-phoneNumber"
          type="tel"
          {...registerField("phoneNumber")}
          className={styles.input}
          placeholder="+380..."
        />
      </div>

      <div className={styles.field}>
        <label className={styles.label} htmlFor="register-password">
          Password
        </label>
        <input
          id="register-password"
          type="password"
          {...registerField("password")}
          className={styles.input}
          placeholder="********"
        />
        {errors.password ? <p className={styles.fieldError}>{errors.password.message}</p> : null}
      </div>

      {error ? <p className={styles.errorMessage}>{error}</p> : null}
      {success ? <p className={styles.successMessage}>{success}</p> : null}

      <button
        type="submit"
        disabled={loading}
        className={styles.submitButton}
      >
        {loading ? "Registering..." : "Register"}
      </button>

      <p className={styles.switchText}>
        Already have an account?{" "}
        <button type="button" onClick={onSwitchToLogin} className={styles.linkButton}>
          Login
        </button>
      </p>
    </form>
  );
}




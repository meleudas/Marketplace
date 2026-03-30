"use client";

import { FormEvent, useState } from "react";
import { useAuth } from "@/hooks/useAuth";
import styles from "./RegisterForm.module.css";

export function RegisterForm() {
  const register = useAuth((state) => state.register);
  const loading = useAuth((state) => state.loading);

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [userName, setUserName] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setSuccess(null);

    const result = await register({
      email: email.trim(),
      password,
      userName: userName.trim(),
      phoneNumber: phoneNumber.trim().length > 0 ? phoneNumber.trim() : null,
    });

    if (!result.success) {
      setError(result.message);
      return;
    }

    setSuccess(result.message);
    setPassword("");
  };

  return (
    <form onSubmit={handleSubmit} className={styles.form}>
      <h2 className={styles.title}>Register</h2>

      <div className={styles.field}>
        <label className={styles.label} htmlFor="register-userName">
          Username
        </label>
        <input
          id="register-userName"
          type="text"
          value={userName}
          onChange={(event) => setUserName(event.target.value)}
          required
          className={styles.input}
          placeholder="john"
        />
      </div>

      <div className={styles.field}>
        <label className={styles.label} htmlFor="register-email">
          Email
        </label>
        <input
          id="register-email"
          type="email"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          required
          className={styles.input}
          placeholder="you@example.com"
        />
      </div>

      <div className={styles.field}>
        <label className={styles.label} htmlFor="register-phoneNumber">
          Phone number (optional)
        </label>
        <input
          id="register-phoneNumber"
          type="tel"
          value={phoneNumber}
          onChange={(event) => setPhoneNumber(event.target.value)}
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
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          required
          className={styles.input}
          placeholder="********"
        />
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
    </form>
  );
}


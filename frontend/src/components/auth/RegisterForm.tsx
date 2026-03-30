"use client";

import { FormEvent, useState } from "react";
import { useAuth } from "@/hooks/useAuth";

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
    <form onSubmit={handleSubmit} className="space-y-4 rounded-xl border border-zinc-200 bg-white p-5 shadow-sm">
      <h2 className="text-xl font-semibold text-zinc-900">Register</h2>

      <div className="space-y-1">
        <label className="text-sm font-medium text-zinc-700" htmlFor="register-userName">
          Username
        </label>
        <input
          id="register-userName"
          type="text"
          value={userName}
          onChange={(event) => setUserName(event.target.value)}
          required
          className="w-full rounded-lg border border-zinc-300 px-3 py-2 text-sm outline-none ring-blue-500 focus:ring"
          placeholder="john"
        />
      </div>

      <div className="space-y-1">
        <label className="text-sm font-medium text-zinc-700" htmlFor="register-email">
          Email
        </label>
        <input
          id="register-email"
          type="email"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          required
          className="w-full rounded-lg border border-zinc-300 px-3 py-2 text-sm outline-none ring-blue-500 focus:ring"
          placeholder="you@example.com"
        />
      </div>

      <div className="space-y-1">
        <label className="text-sm font-medium text-zinc-700" htmlFor="register-phoneNumber">
          Phone number (optional)
        </label>
        <input
          id="register-phoneNumber"
          type="tel"
          value={phoneNumber}
          onChange={(event) => setPhoneNumber(event.target.value)}
          className="w-full rounded-lg border border-zinc-300 px-3 py-2 text-sm outline-none ring-blue-500 focus:ring"
          placeholder="+380..."
        />
      </div>

      <div className="space-y-1">
        <label className="text-sm font-medium text-zinc-700" htmlFor="register-password">
          Password
        </label>
        <input
          id="register-password"
          type="password"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          required
          className="w-full rounded-lg border border-zinc-300 px-3 py-2 text-sm outline-none ring-blue-500 focus:ring"
          placeholder="********"
        />
      </div>

      {error ? <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p> : null}
      {success ? <p className="rounded-md bg-emerald-50 px-3 py-2 text-sm text-emerald-700">{success}</p> : null}

      <button
        type="submit"
        disabled={loading}
        className="w-full rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-blue-500 disabled:cursor-not-allowed disabled:opacity-60"
      >
        {loading ? "Registering..." : "Register"}
      </button>
    </form>
  );
}


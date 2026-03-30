"use client";

import { useEffect } from "react";
import { LoginForm } from "@/components/auth/LoginForm";
import { RegisterForm } from "@/components/auth/RegisterForm";
import { ProfileCard } from "@/components/profile/ProfileCard";
import { useAuth } from "@/hooks/useAuth";

export default function Home() {
  const user = useAuth((state) => state.user);
  const isAuthenticated = useAuth((state) => state.isAuthenticated);
  const loading = useAuth((state) => state.loading);
  const initialized = useAuth((state) => state.initialized);
  const loadMe = useAuth((state) => state.loadMe);
  const logout = useAuth((state) => state.logout);

  useEffect(() => {
    console.log("[AUTH] App mounted, triggering loadMe().");
    void loadMe();
  }, [loadMe]);

  return (
    <main className="mx-auto flex min-h-screen w-full max-w-4xl flex-col items-center justify-center px-4 py-10">
      <div className="w-full space-y-6">
        <header className="space-y-2 text-center">
          <h1 className="text-3xl font-bold text-zinc-900">Auth MVP</h1>
          <p className="text-sm text-zinc-600">
            Next.js + Axios + Zustand + localStorage token strategy.
          </p>
        </header>

        {!initialized || (loading && !isAuthenticated) ? (
          <div className="rounded-xl border border-zinc-200 bg-white p-6 text-center text-sm text-zinc-600 shadow-sm">
            Loading auth state...
          </div>
        ) : null}

        {initialized && !isAuthenticated ? (
          <section className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <LoginForm />
            <RegisterForm />
          </section>
        ) : null}

        {initialized && isAuthenticated && user ? (
          <ProfileCard
            user={user}
            loading={loading}
            onLogout={async () => {
              await logout();
            }}
          />
        ) : null}
      </div>
    </main>
  );
}

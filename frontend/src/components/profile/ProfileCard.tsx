"use client";

import type { CurrentUser } from "@/types/user";

interface ProfileCardProps {
  user: CurrentUser;
  onLogout: () => Promise<void> | void;
  loading?: boolean;
}

export function ProfileCard({ user, onLogout, loading = false }: ProfileCardProps) {
  return (
    <section className="w-full rounded-xl border border-zinc-200 bg-white p-6 shadow-sm">
      <h2 className="text-xl font-semibold text-zinc-900">Profile</h2>
      <p className="mt-1 text-sm text-zinc-500">You are authenticated.</p>

      <div className="mt-4 space-y-2 rounded-lg bg-zinc-50 p-4 text-sm text-zinc-700">
        <p>
          <span className="font-medium">Email:</span> {user.email}
        </p>
        <p>
          <span className="font-medium">Username:</span> {user.userName ?? "-"}
        </p>
        <p>
          <span className="font-medium">Phone:</span> {user.phoneNumber ?? "-"}
        </p>
      </div>

      <button
        type="button"
        onClick={() => {
          console.log("[PROFILE] Logout button clicked.");
          void onLogout();
        }}
        disabled={loading}
        className="mt-5 w-full rounded-lg bg-red-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-red-500 disabled:cursor-not-allowed disabled:opacity-60"
      >
        {loading ? "Logging out..." : "Logout"}
      </button>
    </section>
  );
}


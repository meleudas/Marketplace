import Link from "next/link";

export default function ForgotPasswordPage() {
  return (
    <main className="mx-auto flex min-h-screen w-full max-w-xl items-center justify-center px-4 py-10">
      <section className="w-full rounded-xl border border-zinc-200 bg-white p-6 shadow-sm">
        <h1 className="text-2xl font-semibold text-zinc-900">Forgot password</h1>
        <p className="mt-3 text-sm text-zinc-600">
          This page is a placeholder. Password recovery flow will be connected to
          <code className="mx-1 rounded bg-zinc-100 px-1 py-0.5">/account/forgot-password</code>
          API endpoint.
        </p>

        <div className="mt-6">
          <Link
            href="/"
            className="inline-flex rounded-lg bg-zinc-900 px-4 py-2 text-sm font-semibold text-white transition hover:bg-zinc-700"
          >
            Back to login
          </Link>
        </div>
      </section>
    </main>
  );
}


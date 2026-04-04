"use client";

import { AxiosError } from "axios";
import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import {
  disableEmailTwoFactor,
  enableEmailTwoFactor,
  sendEmailTwoFactorCode,
} from "@/features/auth/api/auth.api";
import { useAuth } from "@/features/auth/model/auth.store";
import type { ProblemDetails } from "@/shared/types/api.types";
import styles from "./SettingsScreen.module.css";

type TwoFactorStatus = "unknown" | "enabled" | "disabled";

const STATUS_STORAGE_KEY = "email-2fa-status";

const getApiErrorMessage = (error: unknown): string => {
  const axiosError = error as AxiosError;
  const responseData = axiosError.response?.data as ProblemDetails | string | undefined;

  if (typeof responseData === "string") {
    return responseData;
  }

  if (responseData && typeof responseData === "object") {
    if (typeof responseData.detail === "string") {
      return responseData.detail;
    }

    if (typeof responseData.title === "string") {
      return responseData.title;
    }
  }

  return "Request failed. Please try again.";
};

const getStatusClassName = (status: TwoFactorStatus): string => {
  if (status === "enabled") {
    return styles.statusEnabled;
  }

  if (status === "disabled") {
    return styles.statusDisabled;
  }

  return styles.statusUnknown;
};

const getStatusLabel = (status: TwoFactorStatus): string => {
  if (status === "enabled") {
    return "Enabled";
  }

  if (status === "disabled") {
    return "Disabled";
  }

  return "Unknown";
};

export function SettingsScreen() {
  const router = useRouter();
  const user = useAuth((state) => state.user);
  const isAuthenticated = useAuth((state) => state.isAuthenticated);
  const initialized = useAuth((state) => state.initialized);
  const loadMe = useAuth((state) => state.loadMe);

  const [status, setStatus] = useState<TwoFactorStatus>("unknown");
  const [code, setCode] = useState("");
  const [isCodeStepVisible, setIsCodeStepVisible] = useState(false);
  const [isSendingCode, setIsSendingCode] = useState(false);
  const [isEnabling, setIsEnabling] = useState(false);
  const [isDisabling, setIsDisabling] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    void loadMe();
  }, [loadMe]);

  useEffect(() => {
    if (initialized && !isAuthenticated) {
      router.replace("/");
    }
  }, [initialized, isAuthenticated, router]);

  useEffect(() => {
    if (!user) {
      return;
    }

    const storedStatus = window.sessionStorage.getItem(`${STATUS_STORAGE_KEY}:${user.id}`);
    if (storedStatus === "enabled" || storedStatus === "disabled") {
      setStatus(storedStatus);
    }
  }, [user]);

  const isBusy = isSendingCode || isEnabling || isDisabling;

  const cannotEnableReason = useMemo(() => {
    if (!user) {
      return null;
    }

    if (!user.isVerified) {
      return "Email must be verified before enabling 2FA.";
    }

    return null;
  }, [user]);

  const persistStatus = (nextStatus: Exclude<TwoFactorStatus, "unknown">) => {
    if (!user) {
      return;
    }

    window.sessionStorage.setItem(`${STATUS_STORAGE_KEY}:${user.id}`, nextStatus);
    setStatus(nextStatus);
  };

  const handleSendCode = async () => {
    setError(null);
    setSuccess(null);
    setIsSendingCode(true);

    try {
      await sendEmailTwoFactorCode();
      setIsCodeStepVisible(true);
      setSuccess("Verification code was sent to your email.");
    } catch (requestError) {
      setError(getApiErrorMessage(requestError));
    } finally {
      setIsSendingCode(false);
    }
  };

  const handleEnable = async () => {
    const normalizedCode = code.trim();
    if (!normalizedCode) {
      setError("Enter verification code first.");
      return;
    }

    setError(null);
    setSuccess(null);
    setIsEnabling(true);

    try {
      await enableEmailTwoFactor(normalizedCode);
      persistStatus("enabled");
      setCode("");
      setIsCodeStepVisible(false);
      setSuccess("Email 2FA enabled.");
    } catch (requestError) {
      setError(getApiErrorMessage(requestError));
    } finally {
      setIsEnabling(false);
    }
  };

  const handleDisable = async () => {
    setError(null);
    setSuccess(null);
    setIsDisabling(true);

    try {
      await disableEmailTwoFactor();
      persistStatus("disabled");
      setCode("");
      setIsCodeStepVisible(false);
      setSuccess("Email 2FA disabled.");
    } catch (requestError) {
      setError(getApiErrorMessage(requestError));
    } finally {
      setIsDisabling(false);
    }
  };

  if (!initialized || !isAuthenticated || !user) {
    return (
      <main className={styles.main}>
        <section className={styles.card}>
          <h1 className={styles.title}>Security Settings</h1>
          <p className={styles.subtitle}>Loading account settings...</p>
        </section>
      </main>
    );
  }

  return (
    <main className={styles.main}>
      <section className={styles.card}>
        <h1 className={styles.title}>Security Settings</h1>
        <p className={styles.subtitle}>Manage email two-factor authentication for your account.</p>

        <div className={styles.section}>
          <h2 className={styles.sectionTitle}>Email two-factor authentication</h2>

          <div className={styles.statusRow}>
            <span>Status:</span>
            <span className={getStatusClassName(status)}>{getStatusLabel(status)}</span>
          </div>

          {!user.isVerified ? (
            <p className={styles.helperText}>Email must be verified before enabling 2FA.</p>
          ) : null}

          {status !== "enabled" ? (
            <div className={styles.actions}>
              <button
                type="button"
                disabled={Boolean(cannotEnableReason) || isBusy}
                className={styles.primaryButton}
                onClick={() => {
                  void handleSendCode();
                }}
              >
                {isSendingCode ? "Sending code..." : "Enable email 2FA"}
              </button>
            </div>
          ) : (
            <div className={styles.actions}>
              <button
                type="button"
                disabled={isBusy}
                className={styles.secondaryButton}
                onClick={() => {
                  void handleDisable();
                }}
              >
                {isDisabling ? "Disabling..." : "Disable email 2FA"}
              </button>
            </div>
          )}

          {isCodeStepVisible ? (
            <div className={styles.inputRow}>
              <input
                type="text"
                value={code}
                onChange={(event) => setCode(event.target.value)}
                placeholder="Enter code from email"
                className={styles.input}
                disabled={isBusy}
              />
              <button
                type="button"
                className={styles.primaryButton}
                disabled={isBusy}
                onClick={() => {
                  void handleEnable();
                }}
              >
                {isEnabling ? "Enabling..." : "Enable"}
              </button>
              <button
                type="button"
                className={styles.ghostButton}
                disabled={isBusy}
                onClick={() => {
                  setIsCodeStepVisible(false);
                  setCode("");
                  setError(null);
                }}
              >
                Cancel
              </button>
            </div>
          ) : null}

          {error ? <p className={styles.error}>{error}</p> : null}
          {success ? <p className={styles.success}>{success}</p> : null}
        </div>

        <button type="button" className={styles.backButton} onClick={() => router.replace("/")}>
          Back to profile
        </button>
      </section>
    </main>
  );
}


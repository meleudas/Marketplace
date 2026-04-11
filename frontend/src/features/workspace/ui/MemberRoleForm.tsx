"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import {
  memberRoleFormSchema,
  memberRoles,
  type MemberRoleFormValues,
} from "@/features/workspace/model/member-role-form.schema";
import type { CompanyWorkspaceRole } from "@/features/workspace/model/workspace.types";
import styles from "@/features/workspace/screens/WorkspaceScreen.module.css";

interface MemberRoleFormProps {
  submitLabel: string;
  busy: boolean;
  defaultRole?: CompanyWorkspaceRole;
  onSubmit: (values: MemberRoleFormValues) => Promise<void>;
}

export function MemberRoleForm({ submitLabel, busy, defaultRole = "seller", onSubmit }: MemberRoleFormProps) {
  const form = useForm<z.input<typeof memberRoleFormSchema>, unknown, MemberRoleFormValues>({
    resolver: zodResolver(memberRoleFormSchema),
    defaultValues: {
      userId: "",
      role: defaultRole,
    },
  });

  return (
    <form className={styles.formInline} onSubmit={form.handleSubmit(onSubmit)}>
      <label className={styles.fieldCompact}>
        User ID
        <input className={styles.input} {...form.register("userId")} />
        {form.formState.errors.userId ? (
          <span className={styles.error}>{form.formState.errors.userId.message}</span>
        ) : null}
      </label>

      <label className={styles.fieldCompact}>
        Role
        <select className={styles.select} {...form.register("role")}>
          {memberRoles.map((role) => (
            <option key={role} value={role}>
              {role}
            </option>
          ))}
        </select>
      </label>

      <button type="submit" className={styles.primaryButton} disabled={busy || form.formState.isSubmitting}>
        {busy || form.formState.isSubmitting ? "Saving..." : submitLabel}
      </button>
    </form>
  );
}

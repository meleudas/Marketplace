"use client";

import { useEffect, useMemo, useState } from "react";
import {
  assignCompanyMemberRole,
  changeCompanyMemberRole,
  getCompanyMembers,
  getCompanyMembershipMe,
  removeCompanyMember,
} from "@/features/workspace/api/members.api";
import { WORKSPACE_COMPANY_ID } from "@/features/workspace/config/workspace.constants";
import { getWorkspaceErrorMessage } from "@/features/workspace/lib/workspace.error";
import { canManageMembers } from "@/features/workspace/model/workspace.permissions";
import type { CompanyMemberDto, CompanyMembershipDto } from "@/features/workspace/model/workspace.types";
import { WorkspaceMembershipError } from "@/features/workspace/model/workspace.types";
import { MemberRoleForm } from "@/features/workspace/ui/MemberRoleForm";
import { useAuth } from "@/features/auth/model/auth.store";
import styles from "./WorkspaceScreen.module.css";

export function WorkspaceMembersScreen() {
  const user = useAuth((state) => state.user);
  const isGlobalAdmin = user?.role === "admin";

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [myMembership, setMyMembership] = useState<CompanyMembershipDto | null>(null);
  const [members, setMembers] = useState<CompanyMemberDto[]>([]);

  const canManage = useMemo(() => canManageMembers(myMembership, user), [myMembership, user]);

  const load = async () => {
    try {
      setLoading(true);
      setError(null);

      let me: CompanyMembershipDto | null = null;

      if (!isGlobalAdmin) {
        me = await getCompanyMembershipMe(WORKSPACE_COMPANY_ID);
      } else {
        try {
          me = await getCompanyMembershipMe(WORKSPACE_COMPANY_ID);
        } catch (membershipError) {
          if (
            !(membershipError instanceof WorkspaceMembershipError) ||
            (membershipError.kind !== "forbidden" && membershipError.kind !== "notFound")
          ) {
            throw membershipError;
          }
        }
      }

      const allMembers = await getCompanyMembers(WORKSPACE_COMPANY_ID);

      setMyMembership(me);
      setMembers(allMembers);
    } catch (loadError) {
      setError(getWorkspaceErrorMessage(loadError, "Failed to load company members"));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, [isGlobalAdmin]);

  const runAction = async (action: () => Promise<void>, successMessage: string) => {
    try {
      setSaving(true);
      setFeedback(null);
      await action();
      setFeedback(successMessage);
      await load();
    } catch (actionError) {
      setFeedback(getWorkspaceErrorMessage(actionError, "Action failed"));
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return <p className={styles.state}>Loading members...</p>;
  }

  if (error) {
    return <p className={styles.state}>{error}</p>;
  }

  return (
    <div className={styles.stack}>
      <section className={styles.card}>
        <h2 className={styles.sectionTitle}>Members</h2>
        <p className={styles.row}>Company ID: {WORKSPACE_COMPANY_ID}</p>
        <p className={styles.row}>My role: {myMembership?.role ?? (isGlobalAdmin ? "admin" : "unknown")}</p>
        <p className={styles.row}>Is owner: {myMembership?.isOwner ? "Yes" : "No"}</p>
        {!canManage ? <p className={styles.hint}>Read-only mode for your role.</p> : null}
        {feedback ? <p className={styles.feedback}>{feedback}</p> : null}

      </section>

      {canManage ? (
        <section className={styles.card}>
          <h3 className={styles.subTitle}>Assign role</h3>
          <MemberRoleForm
            submitLabel="Assign"
            busy={saving}
            onSubmit={async (values) => {
              await runAction(
                async () =>
                  assignCompanyMemberRole(WORKSPACE_COMPANY_ID, values.userId, {
                    role: values.role,
                  }),
                "Role assigned.",
              );
            }}
          />

          <h3 className={styles.subTitle}>Change role</h3>
          <MemberRoleForm
            submitLabel="Change"
            busy={saving}
            onSubmit={async (values) => {
              await runAction(
                async () =>
                  changeCompanyMemberRole(WORKSPACE_COMPANY_ID, values.userId, {
                    role: values.role,
                  }),
                "Role changed.",
              );
            }}
          />
        </section>
      ) : null}

      <section className={styles.card}>
        <h3 className={styles.subTitle}>Company members</h3>
        {members.length === 0 ? (
          <p className={styles.state}>No members found</p>
        ) : (
          <div className={styles.tableWrap}>
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>User ID</th>
                  <th>Role</th>
                  <th>Is owner</th>
                  <th>Created</th>
                  <th>Updated</th>
                  {canManage ? <th>Actions</th> : null}
                </tr>
              </thead>
              <tbody>
                {members.map((member) => (
                  <tr key={member.userId}>
                    <td>{member.userId}</td>
                    <td>{member.role}</td>
                    <td>{member.isOwner ? "Yes" : "No"}</td>
                    <td>{member.createdAt ?? "-"}</td>
                    <td>{member.updatedAt ?? "-"}</td>
                    {canManage ? (
                      <td>
                        <button
                          type="button"
                          className={styles.dangerButton}
                          disabled={saving}
                          onClick={() => {
                            const shouldRemove = window.confirm(
                              `Remove member ${member.userId} from company?`,
                            );

                            if (!shouldRemove) {
                              return;
                            }

                            void runAction(
                              async () => removeCompanyMember(WORKSPACE_COMPANY_ID, member.userId),
                              "Member removed.",
                            );
                          }}
                        >
                          Remove
                        </button>
                      </td>
                    ) : null}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}


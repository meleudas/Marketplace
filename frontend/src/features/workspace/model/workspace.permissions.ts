import type { CompanyMembershipDto, CompanyWorkspaceRole } from "@/features/workspace/model/workspace.types";
import type { UserDto } from "@/shared/types/user.types";

const PRODUCT_WRITE_ROLES: CompanyWorkspaceRole[] = ["owner", "manager", "seller"];
const INVENTORY_WRITE_ROLES: CompanyWorkspaceRole[] = ["owner", "manager", "logistics"];
const MEMBER_MANAGE_ROLES: CompanyWorkspaceRole[] = ["owner", "manager"];

const normalizeWorkspaceRole = (role: string | null | undefined): CompanyWorkspaceRole | null => {
  if (!role) {
    return null;
  }

  const normalized = role.toLowerCase() as CompanyWorkspaceRole;

  return ["owner", "manager", "seller", "support", "logistics"].includes(normalized)
    ? normalized
    : null;
};

export const canWriteProducts = (membership: CompanyMembershipDto | null): boolean => {
  const role = normalizeWorkspaceRole(membership?.role);
  return Boolean(role && PRODUCT_WRITE_ROLES.includes(role));
};

export const canWriteInventory = (membership: CompanyMembershipDto | null): boolean => {
  const role = normalizeWorkspaceRole(membership?.role);
  return Boolean(role && INVENTORY_WRITE_ROLES.includes(role));
};

export const canManageMembers = (membership: CompanyMembershipDto | null, user: UserDto | null): boolean => {
  if (user?.role === "admin") {
    return true;
  }

  const role = normalizeWorkspaceRole(membership?.role);
  return Boolean(role && MEMBER_MANAGE_ROLES.includes(role));
};

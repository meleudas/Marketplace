import type { CompanyMembershipDto, CompanyWorkspaceRole } from "@/features/workspace/model/workspace.types";
import type { UserDto } from "@/shared/types/user.types";

const PRODUCT_WRITE_ROLES: CompanyWorkspaceRole[] = ["owner", "manager", "seller"];
const INVENTORY_WRITE_ROLES: CompanyWorkspaceRole[] = ["owner", "manager", "logistics"];
const MEMBER_MANAGE_ROLES: CompanyWorkspaceRole[] = ["owner", "manager"];

export const canWriteProducts = (membership: CompanyMembershipDto | null): boolean =>
  Boolean(membership && PRODUCT_WRITE_ROLES.includes(membership.role));

export const canWriteInventory = (membership: CompanyMembershipDto | null): boolean =>
  Boolean(membership && INVENTORY_WRITE_ROLES.includes(membership.role));

export const canManageMembers = (membership: CompanyMembershipDto | null, user: UserDto | null): boolean => {
  if (user?.role === "admin") {
    return true;
  }

  return Boolean(membership && MEMBER_MANAGE_ROLES.includes(membership.role));
};


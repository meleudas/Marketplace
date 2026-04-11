import { z } from "zod";

export const memberRoles = ["owner", "manager", "seller", "support", "logistics"] as const;

export const memberRoleFormSchema = z.object({
  userId: z.string().trim().min(1, "User ID is required"),
  role: z.enum(memberRoles),
});

export type MemberRoleFormValues = z.infer<typeof memberRoleFormSchema>;


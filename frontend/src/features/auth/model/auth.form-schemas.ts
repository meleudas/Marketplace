import { z } from "zod";

const requiredText = (label: string) => z.string().trim().min(1, `${label} is required`);

export const loginFormSchema = z.object({
  email: z.email("Enter a valid email").trim(),
  password: requiredText("Password"),
  twoFactorCode: z.string().trim().optional(),
});

export const registerFormSchema = z.object({
  userName: requiredText("Username"),
  email: z.email("Enter a valid email").trim(),
  phoneNumber: z.string().trim().optional(),
  password: requiredText("Password"),
});

export const forgotPasswordFormSchema = z.object({
  email: z.email("Enter a valid email").trim(),
  token: z.string().trim().optional(),
  newPassword: z.string().trim().optional(),
});

export const forgotPasswordResetSchema = z.object({
  email: z.email("Enter a valid email").trim(),
  token: requiredText("Reset token"),
  newPassword: requiredText("New password"),
});

export const confirmEmailQuerySchema = z.object({
  email: z.email("Invalid email").trim(),
  token: requiredText("Invalid token"),
});

export type LoginFormValues = z.infer<typeof loginFormSchema>;
export type RegisterFormValues = z.infer<typeof registerFormSchema>;
export type ForgotPasswordFormValues = z.infer<typeof forgotPasswordFormSchema>;



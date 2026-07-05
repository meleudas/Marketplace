import { z } from "zod";

const requiredText = (label: string) => z.string().trim().min(1, `${label} є обов'язковим`);

export const loginFormSchema = z.object({
  email: z.email("Введіть дійсну електронну адресу").trim(),
  password: requiredText("Пароль"),
  twoFactorCode: z.string().trim().optional(),
});

export const registerFormSchema = z.object({
  userName: requiredText("Ім'я користувача"),
  email: z.email("Введіть дійсну електронну адресу").trim(),
  phoneNumber: z.string().trim().optional(),
  password: requiredText("Пароль"),
});

export const forgotPasswordFormSchema = z.object({
  email: z.email("Введіть дійсну електронну адресу").trim(),
  token: z.string().trim().optional(),
  newPassword: z.string().trim().optional(),
});

export const forgotPasswordResetSchema = z.object({
  email: z.email("Введіть дійсну електронну адресу").trim(),
  token: requiredText("Код скидання"),
  newPassword: requiredText("Новий пароль"),
});

export const confirmEmailQuerySchema = z.object({
  email: z.email("Некоректна електронна адреса").trim(),
  token: requiredText("Некоректний код"),
});

export type LoginFormValues = z.infer<typeof loginFormSchema>;
export type RegisterFormValues = z.infer<typeof registerFormSchema>;
export type ForgotPasswordFormValues = z.infer<typeof forgotPasswordFormSchema>;



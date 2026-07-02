import { z } from "zod";

const ukPhonePattern = /^(\+380|380|0)\s?\(?\d{2}\)?[\s-]?\d{3}[\s-]?\d{2}[\s-]?\d{2}$/;

export const emailFieldDemoSchema = z.object({
  email: z
    .string()
    .trim()
    .min(1, "Email обов'язковий")
    .pipe(z.email("Введіть коректний email")),
});

export const phoneFieldDemoSchema = z.object({
  phone: z
    .string()
    .trim()
    .min(1, "Номер телефону обов'язковий")
    .regex(ukPhonePattern, "Невірний формат номера, наприклад +380 XX XXX XX XX"),
});

export type EmailFieldDemoValues = z.infer<typeof emailFieldDemoSchema>;
export type PhoneFieldDemoValues = z.infer<typeof phoneFieldDemoSchema>;

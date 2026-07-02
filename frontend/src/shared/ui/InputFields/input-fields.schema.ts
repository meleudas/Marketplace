import { z } from "zod";

const ukPhonePattern = /^(\+380|380|0)\s?\(?\d{2}\)?[\s-]?\d{3}[\s-]?\d{2}[\s-]?\d{2}$/;

export const emailFieldSchema = z.object({
  email: z
    .string()
    .trim()
    .min(1, "Email обов'язковий")
    .pipe(z.email("Введіть коректний email")),
});

export const phoneFieldSchema = z.object({
  phone: z
    .string()
    .trim()
    .min(1, "Номер телефону обов'язковий")
    .regex(ukPhonePattern, "Невірний формат номера, наприклад +380 XX XXX XX XX"),
});

export type EmailFieldValues = z.infer<typeof emailFieldSchema>;
export type PhoneFieldValues = z.infer<typeof phoneFieldSchema>;

"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Button } from "../Button";
import { TextField } from "../TextField";
import { Typography } from "../Typography";
import {
  emailFieldSchema,
  phoneFieldSchema,
  type EmailFieldValues,
  type PhoneFieldValues,
} from "./input-fields.schema";
import styles from "./InputFields.module.css";

export function InputFields() {
  const [emailSuccess, setEmailSuccess] = useState<string | null>(null);
  const [phoneSuccess, setPhoneSuccess] = useState<string | null>(null);

  const emailForm = useForm<EmailFieldValues>({
    resolver: zodResolver(emailFieldSchema),
    defaultValues: { email: "" },
    mode: "onBlur",
    reValidateMode: "onBlur",
  });

  const phoneForm = useForm<PhoneFieldValues>({
    resolver: zodResolver(phoneFieldSchema),
    defaultValues: { phone: "" },
    mode: "onBlur",
    reValidateMode: "onBlur",
  });

  const onEmailSubmit = (values: EmailFieldValues) => {
    setEmailSuccess(`Email прийнято: ${values.email}`);
  };

  const onPhoneSubmit = (values: PhoneFieldValues) => {
    setPhoneSuccess(`Номер прийнято: ${values.phone}`);
  };

  return (
    <div className={styles.showcase}>
      <form className={styles.form} noValidate onSubmit={emailForm.handleSubmit(onEmailSubmit)}>
        <TextField
          label="Email"
          kind="email"
          placeholder="you@example.com"
          error={emailForm.formState.errors.email?.message}
          {...emailForm.register("email", {
            onChange: () => setEmailSuccess(null),
          })}
        />
        <Button type="submit" variant="primary" size="sm">
          Перевірити email
        </Button>
        {emailSuccess ? (
          <Typography variant="body2" className={styles.success}>
            {emailSuccess}
          </Typography>
        ) : null}
      </form>

      <form className={styles.form} noValidate onSubmit={phoneForm.handleSubmit(onPhoneSubmit)}>
        <TextField
          label="Телефон"
          kind="tel"
          placeholder="+380 XX XXX XX XX"
          error={phoneForm.formState.errors.phone?.message}
          {...phoneForm.register("phone", {
            onChange: () => setPhoneSuccess(null),
          })}
        />
        <Button type="submit" variant="primary" size="sm">
          Перевірити телефон
        </Button>
        {phoneSuccess ? (
          <Typography variant="body2" className={styles.success}>
            {phoneSuccess}
          </Typography>
        ) : null}
      </form>
    </div>
  );
}

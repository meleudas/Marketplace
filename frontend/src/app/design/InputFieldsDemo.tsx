"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Button, TextField, Typography } from "@/shared/ui";
import {
  emailFieldDemoSchema,
  phoneFieldDemoSchema,
  type EmailFieldDemoValues,
  type PhoneFieldDemoValues,
} from "./input-fields-demo.schema";
import styles from "./UiKitShowcase.module.css";

export function InputFieldsDemo() {
  const [emailSuccess, setEmailSuccess] = useState<string | null>(null);
  const [phoneSuccess, setPhoneSuccess] = useState<string | null>(null);

  const emailForm = useForm<EmailFieldDemoValues>({
    resolver: zodResolver(emailFieldDemoSchema),
    defaultValues: { email: "" },
    mode: "onBlur",
    reValidateMode: "onBlur",
  });

  const phoneForm = useForm<PhoneFieldDemoValues>({
    resolver: zodResolver(phoneFieldDemoSchema),
    defaultValues: { phone: "" },
    mode: "onBlur",
    reValidateMode: "onBlur",
  });

  const onEmailSubmit = (values: EmailFieldDemoValues) => {
    setEmailSuccess(`Email прийнято: ${values.email}`);
  };

  const onPhoneSubmit = (values: PhoneFieldDemoValues) => {
    setPhoneSuccess(`Номер прийнято: ${values.phone}`);
  };

  return (
    <div className={styles.inputShowcase}>
      <form
        className={styles.inputForm}
        noValidate
        onSubmit={emailForm.handleSubmit(onEmailSubmit)}
      >
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
          <Typography variant="body2" className={styles.inputSuccess}>
            {emailSuccess}
          </Typography>
        ) : null}
      </form>

      <form
        className={styles.inputForm}
        noValidate
        onSubmit={phoneForm.handleSubmit(onPhoneSubmit)}
      >
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
          <Typography variant="body2" className={styles.inputSuccess}>
            {phoneSuccess}
          </Typography>
        ) : null}
      </form>
    </div>
  );
}

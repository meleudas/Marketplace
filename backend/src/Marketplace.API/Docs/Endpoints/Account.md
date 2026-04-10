# AccountController — `/account`

## `POST /account/confirm-email`

- **Призначення:** підтвердження email (Identity + доменний `User.Verify()`).
- **Приймає:** body `ConfirmEmailRequest` (`email`, `token`).
- **Повертає:** **200** порожнє тіло при успіху.
- **Авторизація:** не потрібна.
- **Side effects:** `EmailConfirmed`, `isVerified` у профілі маркетплейсу.
- **Помилки:** невалідний/прострочений токен.

## `POST /account/forgot-password`

- **Призначення:** запит листа зі скиданням пароля.
- **Приймає:** body `ForgotPasswordRequest` (`email`).
- **Повертає:** **200** порожнє тіло (зазвичай без розкриття існування email).
- **Авторизація:** не потрібна.

## `POST /account/reset-password`

- **Призначення:** встановити новий пароль за токеном з листа.
- **Приймає:** body `ResetPasswordRequest` (`email`, `token`, `newPassword`).
- **Повертає:** **200** порожнє тіло.
- **Авторизація:** не потрібна.

## `GET /account/2fa/status`

- **Призначення:** поточний стан 2FA (email/Telegram тощо) для акаунта.
- **Приймає:** немає.
- **Повертає:** **200** `TwoFactorStatusDto` (структура з `IAuthenticationPort`).
- **Авторизація:** **Bearer**.
- **Помилки:** **401** без user id.

## `POST /account/2fa/email/send-code`

- **Призначення:** надіслати одноразовий код на email.
- **Повертає:** **200** порожнє тіло.
- **Авторизація:** **Bearer**.

## `POST /account/2fa/email/enable`

- **Приймає:** body `EnableEmailTwoFactorRequest` (`code`).
- **Повертає:** **200** порожнє тіло.
- **Авторизація:** **Bearer**.

## `POST /account/2fa/email/disable`

- **Повертає:** **200** порожнє тіло.
- **Авторизація:** **Bearer**.

## `POST /account/2fa/telegram/link-code`

- **Призначення:** видати `linkCode` для `/start <linkCode>` у боті.
- **Повертає:** **200** `{ linkCode }` (`TelegramLinkCodeResponse`).
- **Авторизація:** **Bearer**.

## `POST /account/2fa/telegram/send-code`

- **Повертає:** **200** порожнє тіло (Telegram уже прив’язаний).
- **Авторизація:** **Bearer**.

## `POST /account/2fa/telegram/enable`

- **Приймає:** body `EnableTelegramTwoFactorRequest` (`code`).
- **Повертає:** **200** порожнє тіло.
- **Авторизація:** **Bearer**.

## `POST /account/2fa/telegram/disable`

- **Повертає:** **200** порожнє тіло.
- **Авторизація:** **Bearer**.

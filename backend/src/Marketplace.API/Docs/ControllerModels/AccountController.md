# AccountController

Контролер: `/account/*`

## Авторизація
- `POST /account/confirm-email` - `AllowAnonymous`
- `POST /account/forgot-password` - `AllowAnonymous`
- `POST /account/reset-password` - `AllowAnonymous`
- Решта `/account/2fa/*` - `Authorize` (будь-який валідний JWT)

## Моделі запитів (copy-paste)

### POST `/account/confirm-email`
```json
{
  "email": "user@example.com",
  "token": "email_confirmation_token"
}
```

### POST `/account/forgot-password`
```json
{
  "email": "user@example.com"
}
```

### POST `/account/reset-password`
```json
{
  "email": "user@example.com",
  "token": "reset_password_token",
  "newPassword": "Admin123!"
}
```

### GET `/account/2fa/status`
- Body не потрібен.
- Відповідь: `TwoFactorStatusDto` (див. OpenAPI / [Endpoints/Account.md](../Endpoints/Account.md)).

### POST `/account/2fa/email/send-code`
- Body не потрібен.

### POST `/account/2fa/email/enable`
```json
{
  "code": "123456"
}
```

### POST `/account/2fa/email/disable`
- Body не потрібен.

### POST `/account/2fa/telegram/link-code`
- Body не потрібен.

### POST `/account/2fa/telegram/send-code`
- Body не потрібен.

### POST `/account/2fa/telegram/enable`
```json
{
  "code": "123456"
}
```

### POST `/account/2fa/telegram/disable`
- Body не потрібен.

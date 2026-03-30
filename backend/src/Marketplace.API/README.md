# Marketplace.API — документація ендпоінтів

Цей файл описує поточний HTTP API: які сутності (DTO) приймає, які повертає, які ендпоінти потребують авторизації та як працюють токени/cookie.

Базовий URL у локальному середовищі (Docker): `http://localhost:8080`

## Загальні правила

- Формат тіла запитів/відповідей: `application/json`
- Авторизація: `Authorization: Bearer <access_token>`
- Access token повертається у JSON
- Refresh token ставиться в HTTPOnly cookie (назва за замовчуванням: `refresh_token`)
- Більшість помилок повертаються як `ProblemDetails`:
  - `title`
  - `detail`
  - `status`

## DTO (сутності запитів/відповідей)

### Auth DTO

`RegisterRequest`

- `email: string`
- `password: string`
- `userName: string`
- `phoneNumber: string | null`

`LoginRequest`

- `email: string`
- `password: string`
- `rememberMe: boolean`
- `twoFactorCode: string | null` (6 цифр)

`RefreshRequest`

- `refreshToken: string | null`

`AuthTokensDto` (успішний `register/login/refresh`)

- `accessToken: string`
- `refreshToken: string`
- `accessTokenExpiresAt: string (ISO datetime)`
- `refreshTokenExpiresAt: string (ISO datetime)`

### Account DTO

`ConfirmEmailRequest`

- `email: string`
- `token: string`

`ForgotPasswordRequest`

- `email: string`

`ResetPasswordRequest`

- `email: string`
- `token: string`
- `newPassword: string`

`EnableEmailTwoFactorRequest`

- `code: string` (6 цифр)

### Users DTO

`UserDto`

- `id: string (guid)`
- `firstName: string`
- `lastName: string`
- `role: string` (`buyer | seller | moderator | admin`)
- `birthday: string | null`
- `avatar: string | null`
- `isVerified: boolean`
- `verificationDocument: string | null`
- `lastLoginAt: string | null`
- `createdAt: string`
- `updatedAt: string`
- `isDeleted: boolean`
- `deletedAt: string | null`

### OAuth DTO

`GoogleCallbackExchangeRequest`

- `code: string`

Відповідь успішного обміну коду (`POST /auth/google/callback`)

- `accessToken: string`
- `accessTokenExpiresAt: string (ISO datetime)`

## Ендпоінти

## AuthController (`/auth`)

### `POST /auth/register`

Створює акаунт, повертає токени.

- **Auth:** не потрібна
- **Request body:** `RegisterRequest`
- **200 OK body:** `AuthTokensDto`
- **Cookie:** встановлюється refresh cookie

### `POST /auth/login`

Логін по email/password.

- **Auth:** не потрібна
- **Request body:** `LoginRequest`
- **200 OK body:** `AuthTokensDto`
- **Cookie:** встановлюється refresh cookie

### `POST /auth/refresh`

Оновлює токени по refresh token.

- **Auth:** не потрібна
- **Request body:** `RefreshRequest` (можна передати `null`)
- **Fallback:** якщо в body немає токена, API пробує взяти його з cookie
- **200 OK body:** `AuthTokensDto`
- **Cookie:** перевстановлюється refresh cookie

### `POST /auth/logout`

Логаут користувача.

- **Auth:** потрібна (`Bearer`)
- **Request body:** немає
- **200 OK body:** порожній
- **Cookie:** refresh cookie видаляється

## ExternalAuthController (`/auth`)

### `GET /auth/google`

Старт OAuth2 flow з Google.

- **Auth:** не потрібна
- **Query:** `returnPath` (опційно, за замовчуванням `/auth/callback`)
- **Response:** `302 Redirect` на Google consent screen

### `GET /auth/google/return`

Внутрішній callback бекенду після Google.

- **Auth:** не потрібна
- **Query:** `appState`
- **Response:** `302 Redirect` на фронтенд з тимчасовим `code` у query

> Це технічний endpoint для flow. Викликається редіректом після Google sign-in.

### `POST /auth/google/callback`

Обмінює тимчасовий `code` на локальний access token + ставить refresh cookie.

- **Auth:** не потрібна
- **Request body:** `GoogleCallbackExchangeRequest`
- **200 OK body:** `{ accessToken, accessTokenExpiresAt }`
- **Cookie:** встановлюється refresh cookie

## AccountController (`/account`)

### `POST /account/confirm-email`

Підтвердження email токеном.

- **Auth:** не потрібна
- **Request body:** `ConfirmEmailRequest`
- **200 OK body:** порожній

### `POST /account/forgot-password`

Запит на скидання пароля.

- **Auth:** не потрібна
- **Request body:** `ForgotPasswordRequest`
- **200 OK body:** порожній

### `POST /account/reset-password`

Скидання пароля токеном.

- **Auth:** не потрібна
- **Request body:** `ResetPasswordRequest`
- **200 OK body:** порожній

### `POST /account/2fa/email/send-code`

Надсилає одноразовий код 2FA на email поточного користувача.

- **Auth:** потрібна
- **Request body:** немає
- **200 OK body:** порожній

### `POST /account/2fa/email/enable`

Вмикає 2FA для акаунта після перевірки коду.

- **Auth:** потрібна
- **Request body:** `EnableEmailTwoFactorRequest`
- **200 OK body:** порожній

### `POST /account/2fa/email/disable`

Вимикає email 2FA для поточного користувача.

- **Auth:** потрібна
- **Request body:** немає
- **200 OK body:** порожній

## UsersController (`/users`)

> Усі ендпоінти цього контролера вимагають `Bearer` токен.

### `GET /users/me`

Повертає профіль поточного користувача.

- **Auth:** потрібна
- **200 OK body:** `UserDto`

### `GET /users`

Повертає список користувачів.

- **Auth:** потрібна
- **200 OK body:** `UserDto[]`

### `GET /users/search?userName=<value>`

Пошук користувачів за username.

- **Auth:** потрібна
- **Query:** `userName`
- **200 OK body:** `UserDto[]`

### `DELETE /users/{id}`

Видалення акаунта (тільки власний id).

- **Auth:** потрібна
- **Path:** `id` (guid)
- **200 OK body:** порожній
- **403 Forbidden:** якщо `id` не збігається з поточним користувачем

## Сервісні ендпоінти

### `GET /health`

- **Auth:** не потрібна
- **200 OK body:** `{ status: "ok" }`

## Швидкі приклади

`POST /auth/login`

```json
{
  "email": "user@example.com",
  "password": "StrongPassword123!"
}
```

Успішна відповідь:

```json
{
  "accessToken": "eyJ...",
  "refreshToken": "r_...",
  "accessTokenExpiresAt": "2026-03-29T18:40:00Z",
  "refreshTokenExpiresAt": "2026-04-28T18:30:00Z"
}
```

Помилка (`ProblemDetails`):

```json
{
  "title": "Неавторизовано",
  "detail": "Invalid email or password",
  "status": 401
}
```

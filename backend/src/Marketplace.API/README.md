# Marketplace.API — опис HTTP API

Документ описує всі публічні маршрути з `Marketplace.API` та `Program.cs`: призначення, коли викликати, формат вхідних даних, успішні відповіді та що відбувається, якщо тіло відповіді порожнє.

**Базовий URL (приклад, Docker):** `http://localhost:8080`  
**Формат:** `application/json`, крім редіректів OAuth.

## Загальні правила

| Тема | Опис |
|------|------|
| Авторизація | Заголовок `Authorization: Bearer <accessToken>` для ендпоінтів з `[Authorize]`. |
| Access token | Зазвичай у JSON після `login`, `refresh`, `register` (якщо дозволено), обміну Google-коду. |
| Refresh token | Дублюється в тілі відповіді **і** в HTTP-only cookie `refresh_token` (ім’я з `CookieAuthOptions`). `POST /auth/refresh` може взяти refresh з cookie, якщо в body немає значення. |
| Помилки | Частіше `ProblemDetails`: `title`, `detail`, `status`. Коди залежать від тексту `detail` (див. `ResultExtensions`). |
| Підтвердження email | У проєкті ввімкнено `RequireConfirmedEmail` — після реєстрації токени **не** видаються, логін без `EmailConfirmed` блокується (див. `POST /auth/register` та `POST /auth/login`). |

---

## Довідник полів (JSON)

Імена властивостей у JSON — **camelCase** (стандарт ASP.NET Core).

### Реєстрація та сесія

**`RegisterRequest`** (body `POST /auth/register`)

| Поле | Тип | Опис |
|------|-----|------|
| `email` | string | Email акаунта. |
| `password` | string | Пароль (правила — у Identity, мін. довжина 8, цифра). |
| `userName` | string | Ім’я користувача (у т.ч. для розбиття на ім’я/прізвище в домені). |
| `phoneNumber` | string \| null | Опційно. |

**`LoginRequest`** (body `POST /auth/login`)

| Поле | Тип | Опис |
|------|-----|------|
| `email` | string | Email. |
| `password` | string | Пароль. |
| `rememberMe` | boolean | Опційно, за замовчуванням `false` (зарезервовано під політику сесії). |
| `twoFactorCode` | string \| null | Код 2FA з email/Telegram, якщо для акаунта увімкнено 2FA і бекенд попросив код другим кроком. |

**`RefreshRequest`** (body `POST /auth/refresh`, опційно)

| Поле | Тип | Опис |
|------|-----|------|
| `refreshToken` | string \| null | Якщо `null`/відсутнє — береться cookie `refresh_token`. |

**`AuthTokensDto`** (успіх `register` / `login` / `refresh`, коли токени видані)

| Поле | Тип | Опис |
|------|-----|------|
| `accessToken` | string | JWT для `Authorization: Bearer`. |
| `refreshToken` | string | Рядок refresh (також у cookie). |
| `accessTokenExpiresAt` | string (ISO 8601) | Закінчення access. |
| `refreshTokenExpiresAt` | string (ISO 8601) | Закінчення refresh. |

### Акаунт (email, пароль, 2FA)

**`ConfirmEmailRequest`** — `POST /account/confirm-email`

| Поле | Тип | Опис |
|------|-----|------|
| `email` | string | Той самий email, що реєстрували. |
| `token` | string | Токен підтвердження з листа (не JWT access). |

**`ForgotPasswordRequest`** — `POST /account/forgot-password`

| Поле | Тип | Опис |
|------|-----|------|
| `email` | string | Email для листа зі скиданням. |

**`ResetPasswordRequest`** — `POST /account/reset-password`

| Поле | Тип | Опис |
|------|-----|------|
| `email` | string | Email акаунта. |
| `token` | string | Токен з листа скидання пароля. |
| `newPassword` | string | Новий пароль. |

**`EnableEmailTwoFactorRequest`** — `POST /account/2fa/email/enable`

| Поле | Тип | Опис |
|------|-----|------|
| `code` | string | Одноразовий код після `.../2fa/email/send-code`. |

**`EnableTelegramTwoFactorRequest`** — `POST /account/2fa/telegram/enable`

| Поле | Тип | Опис |
|------|-----|------|
| `code` | string | Код після `.../2fa/telegram/send-code` (прив’язаний Telegram). |

**`TelegramLinkCodeResponse`** — відповідь `POST /account/2fa/telegram/link-code`

| Поле | Тип | Опис |
|------|-----|------|
| `linkCode` | string | Код для команди боту `/start <linkCode>`. |

### Користувачі маркетплейсу

**`UserDto`** — `GET /users/me`, елементи списків `GET /users`, `GET /users/search`

| Поле | Тип | Опис |
|------|-----|------|
| `id` | guid | Співпадає з Identity user id. |
| `firstName` | string | |
| `lastName` | string | |
| `role` | string | Значення enum у нижньому регістрі: `buyer`, `seller`, `moderator`, `admin`. |
| `birthday` | string \| null | ISO дата або null. |
| `avatar` | string \| null | URL або null. |
| `isVerified` | boolean | Оновлюється після успішного підтвердження email (доменний профіль). |
| `verificationDocument` | string \| null | |
| `lastLoginAt` | string \| null | |
| `createdAt` | string | |
| `updatedAt` | string | |
| `isDeleted` | boolean | |
| `deletedAt` | string \| null | |

### Google OAuth

**`GoogleCallbackExchangeRequest`** — `POST /auth/google/callback`

| Поле | Тип | Опис |
|------|-----|------|
| `code` | string | Одноразовий код з query після редіректу з `GET /auth/google/return`. |

### Telegram webhook

**`TelegramUpdate`** — body `POST /integrations/telegram/webhook`

| Поле | Тип | Опис |
|------|-----|------|
| `message` | об’єкт \| null | Див. `TelegramMessage`. |

**`TelegramMessage`**

| Поле | Тип |
|------|-----|
| `chat` | `TelegramChat` \| null |
| `text` | string \| null |

**`TelegramChat`**

| Поле | Тип |
|------|-----|
| `id` | number (long) |

---

## Ендпоінти за контролерами

### `AuthController` — префікс `/auth`

#### `POST /auth/register`

- **Що робить:** створює користувача в ASP.NET Identity і запис профілю в `marketplace_users`, надсилає лист із токеном підтвердження email.
- **Коли використовувати:** екран реєстрації в застосунку.
- **Авторизація:** не потрібна.
- **Body:** `RegisterRequest` (див. таблицю вище).
- **Якщо увімкнено підтвердження email (`RequireConfirmedEmail`):** **немає** `200` з токенами. Повертається помилка з `detail` на кшталт «Registration successful. Please confirm your email before login.», статус **403**; **cookie refresh не виставляється**. Клієнт має показати інструкцію «підтвердіть пошту», потім `POST /account/confirm-email` і далі `POST /auth/login`.
- **Якщо підтвердження email вимкнено (нетипова конфігурація):** **200** і тіло `AuthTokensDto`; встановлюється refresh-cookie (як при логіні).

#### `POST /auth/login`

- **Що робить:** перевіряє email/пароль; за потреби вимагає 2FA; оновлює `lastLogin` у доменному користувачі; видає пару access/refresh.
- **Коли використовувати:** вхід після підтвердженого email (якщо увімкнено `RequireConfirmedEmail`).
- **Авторизація:** не потрібна.
- **Body:** `LoginRequest`.
- **Успіх (200):** `AuthTokensDto` + refresh-cookie.
- **Без повного входу (2FA):** `ProblemDetails`, текст містить «2FA code required» (зазвичай **401**); клієнт повторює `login` з тим самим паролем і полем `twoFactorCode`.
- **Непідтверджений email:** повідомлення на кшталт «Please confirm your email before login.» (**403**).

#### `POST /auth/refresh`

- **Що робить:** за валідним refresh (hash у БД, не прострочений, не відкликаний) видає новий access і новий refresh, старий refresh ревокається.
- **Коли використовувати:** коли access JWT закінчився, а сесія має залишитись; зручно з cookie або з явним `refreshToken` у body.
- **Авторизація:** не потрібна.
- **Body:** `RefreshRequest` або порожній об’єкт / опускається — тоді refresh з cookie.
- **Успіх (200):** `AuthTokensDto` + оновлений refresh-cookie.
- **Непідтверджений email:** як і при логіні, refresh блокується з відповідним `detail` (**403**).

#### `POST /auth/logout`

- **Що робить:** ревокує активні refresh-токени користувача в БД; **видаляє** refresh-cookie.
- **Коли використовувати:** явний вихід з акаунта на клієнті.
- **Авторизація:** **Bearer** (потрібен валідний access).
- **Body:** немає.
- **Успіх (200):** **порожнє тіло** (`Ok()`). Після відповіді клієнт має скинути збережений access token локально; серверні refresh для цього користувача більше не приймаються.

---

### `ExternalAuthController` — префікс `/auth` (Google)

#### `GET /auth/google`

- **Що робить:** ініціює OAuth Google (`Challenge`), зберігає внутрішній `appState` з `returnPath` для фронту.
- **Коли використовувати:** кнопка «Увійти через Google» — браузерний перехід на цей URL.
- **Авторизація:** не потрібна.
- **Query:** `returnPath` (опційно, за замовчуванням `"/auth/callback"` на фронті) — **має починатися з `/`**.
- **Успіх:** **302** на сторінку згоди Google. Якщо Google не налаштовано — **503** `ProblemDetails`.

#### `GET /auth/google/return`

- **Що робить:** внутрішній callback після Google; зчитує зовнішній principal, створює/оновлює локального користувача, генерує **одноразовий exchange `code`**, редіректить на `Frontend:BaseUrl` + `returnPath` з `?code=...`.
- **Коли використовувати:** не викликається вручну з фронту API-клієнтом — лише редірект браузера після Google.
- **Query:** `appState` (обов’язковий).
- **Успіх:** **302** на фронт. Помилки — **400/401/500** з `ProblemDetails` (невалідний state, зовнішня автентифікація, відсутній `Frontend:BaseUrl`).

#### `POST /auth/google/callback`

- **Що робить:** обмінює короткоживучий `code` на access JWT, зберігає refresh у БД і в cookie (email для Google вважається підтвердженим).
- **Коли використовувати:** фронт після landing з `?code=` з попереднього кроку викликає цей POST.
- **Body:** `GoogleCallbackExchangeRequest`.
- **Успіх (200):** JSON `{ "accessToken", "accessTokenExpiresAt" }` (camelCase) **без** `refreshToken` у JSON; refresh лише в **cookie**.
- **Помилки:** **400** якщо немає `code`; **401** якщо код прострочений/невалідний.

---

### `AccountController` — префікс `/account`

#### `POST /account/confirm-email`

- **Що робить:** валідує токен Identity, виставляє `EmailConfirmed`, для доменного `User` викликає `Verify()` (прапор `isVerified`).
- **Коли використовувати:** після реєстрації, коли користувач ввів токен з листа або перейшов з посилання, яке ваш фронт перетворює на виклик API.
- **Авторизація:** не потрібна.
- **Body:** `ConfirmEmailRequest`.
- **Успіх (200):** **порожнє тіло**. Наслідок: можна викликати `POST /auth/login`; у профілі `isVerified` стане `true` після цього кроку.

#### `POST /account/forgot-password`

- **Що робить:** за наявного користувача генерує токен скидання і надсилає email (реалізація `IEmailPort`).
- **Коли використовувати:** форма «забув пароль».
- **Body:** `ForgotPasswordRequest`.
- **Успіх (200):** **порожнє тіло** (не розкриває, чи існує email — перевірте поведінку handler за бажанням).

#### `POST /account/reset-password`

- **Що робить:** застосовує новий пароль за токеном з листа.
- **Коли використовувати:** екран встановлення нового пароля після листа.
- **Body:** `ResetPasswordRequest`.
- **Успіх (200):** **порожнє тіло**; далі користувач може логінитись з `newPassword`.

#### `POST /account/2fa/email/send-code`

- **Що робить:** генерує одноразовий код 2FA (Identity, email-провайдер) і надсилає на email акаунта.
- **Коли використовувати:** перед `.../enable`, щоб користувач отримав код.
- **Авторизація:** **Bearer**.
- **Body:** немає.
- **Успіх (200):** **порожнє тіло**; код у пошті.

#### `POST /account/2fa/email/enable`

- **Що робить:** перевіряє код і вмикає 2FA по email для акаунта.
- **Авторизація:** **Bearer**.
- **Body:** `EnableEmailTwoFactorRequest`.
- **Успіх (200):** **порожнє тіло**; наступні логіни можуть вимагати `twoFactorCode`.

#### `POST /account/2fa/email/disable`

- **Що робить:** вимикає сценарій 2FA через email (логіка в `IdentityAuthService`).
- **Авторизація:** **Bearer**.
- **Body:** немає.
- **Успіх (200):** **порожнє тіло**.

#### `POST /account/2fa/telegram/link-code`

- **Що робить:** видає короткоживучий `linkCode` для прив’язки чату Telegram до акаунта (далі користувач пише боту `/start <linkCode>`).
- **Авторизація:** **Bearer**.
- **Body:** немає.
- **Успіх (200):** `TelegramLinkCodeResponse`.

#### `POST /account/2fa/telegram/send-code`

- **Що робить:** надсилає код 2FA у прив’язаний Telegram.
- **Авторизація:** **Bearer**; Telegram має бути вже прив’язаний.
- **Body:** немає.
- **Успіх (200):** **порожнє тіло**.

#### `POST /account/2fa/telegram/enable`

- **Що робить:** перевіряє код і вмикає 2FA через Telegram.
- **Авторизація:** **Bearer**.
- **Body:** `EnableTelegramTwoFactorRequest`.
- **Успіх (200):** **порожнє тіло**.

#### `POST /account/2fa/telegram/disable`

- **Що робить:** вимикає Telegram 2FA (і за потреби коригує загальний прапор 2FA — див. сервіс).
- **Авторизація:** **Bearer**.
- **Body:** немає.
- **Успіх (200):** **порожнє тіло**.

---

### `TelegramIntegrationsController` — префікс `/integrations/telegram`

#### `POST /integrations/telegram/webhook`

- **Що робить:** приймає update від Telegram; якщо текст повідомлення має вигляд `/start <linkCode>`, викликає прив’язку акаунта до чату. Інші update тихо ігноруються з **200**.
- **Коли використовувати:** лише як URL webhook у налаштуваннях Bot API (не з фронту магазину).
- **Авторизація:** якщо задано `Telegram:WebhookSecret`, заголовок **`X-Telegram-Bot-Api-Secret-Token`** має збігатися; інакше **401**.
- **Body:** `TelegramUpdate` (підмножина полів реального Telegram JSON достатня для `message.chat.id` та `message.text`).
- **Успіх (200):** **порожнє тіло** — означає «update прийнято» або «не підходить під `/start` — ігнор». При невдалій прив’язці код зараз повертає **400** без `ProblemDetails` (лише статус).

---

### `UsersController` — префікс `/users`

Усі маршрути вимагають **Bearer**.

#### `GET /users/me`

- **Що робить:** повертає профіль маркетплейсу для поточного Identity id.
- **Коли використовувати:** сторінка «мій профіль», ініціалізація стану після логіну.
- **Успіх (200):** один об’єкт `UserDto`.

#### `GET /users`

- **Що робить:** список користувачів (адміністративний/сервісний сценарій — уточнюйте політику доступу на рівні продукту).
- **Успіх (200):** масив `UserDto` (може бути порожнім).

#### `GET /users/search?userName=...`

- **Що робить:** пошук за іменем (частковий матч у репозиторії).
- **Query:** `userName` (рядок).
- **Успіх (200):** масив `UserDto`.

#### `DELETE /users/{id}`

- **Що робить:** м’яке видалення акаунта **лише якщо `id` збігається** з поточним користувачем; інакше **403**.
- **Коли використовувати:** «видалити мій акаунт».
- **Path:** `id` — guid.
- **Успіх (200):** **порожнє тіло**; обліковий запис позначається видаленим за логікою сервісу.

---

## Мінімальні API з `Program.cs` (не контролери)

#### `GET /health`

- **Що робить:** liveness перевірка.
- **Коли:** балансувальник, Docker healthcheck.
- **Успіх (200):** `{ "status": "ok" }`.

#### `GET /health/sendgrid`

- **Що робить:** перевіряє доступність SendGrid API (ключ).
- **Успіх (200):** об’єкт статусу з `IEmailHealthProbe`.  
- **Помилка (503):** `ProblemDetails`, якщо провайдер «нездоровий».

#### `GET /health/sendgrid/key-trace`

- **Що робить:** діагностика джерел API-ключа SendGrid (env / конфіг / options): масковані довжини, fingerprint, чи збігаються значення.
- **Коли:** лише налагодження (не для публічного інтернету без захисту).
- **Успіх (200):** JSON з вкладеними snapshot-об’єктами для `env`, `config`, `options`.

---

## Документація OpenAPI / Swagger

- Swagger UI: зазвичай `/swagger`
- OpenAPI JSON (Scalar): `/openapi/v1.json` (за поточним мапінгом `MapOpenApi`)

У Swagger для Bearer вводьте **лише JWT** (префікс `Bearer` додає UI).

---

## Короткий приклад логіну

**Запит:** `POST /auth/login`

```json
{
  "email": "user@example.com",
  "password": "StrongPassword123!"
}
```

**Відповідь 200:** тіло у форматі `AuthTokensDto` + Set-Cookie для refresh.

**Помилка:** `ProblemDetails`, наприклад статус **401** для невірого пароля або **403**, якщо email ще не підтверджено.

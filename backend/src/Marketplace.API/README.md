# Marketplace.API — опис HTTP API

Документ описує всі публічні маршрути з `Marketplace.API` та `Program.cs`: призначення, коли викликати, формат вхідних даних, успішні відповіді та що відбувається, якщо тіло відповіді порожнє.

## Канонічна структура документації (оновлено)

| Що потрібно | Де лежить |
|-------------|-----------|
| **Повний довідник endpoint-ів** (приймає / повертає / авторизація / side effects / помилки) | [Docs/README.md](Docs/README.md) → [Docs/Endpoints/README.md](Docs/Endpoints/README.md) |
| **Матриця ролей і DDD-логіка** | [Docs/DDD/README.md](Docs/DDD/README.md), [Docs/DDD/RoleAccessMatrix.md](Docs/DDD/RoleAccessMatrix.md), [Docs/DDD/BusinessFlows.md](Docs/DDD/BusinessFlows.md) |
| **Тестові сутності та сценарії** | [Docs/DDD/TestEntitiesAndScenarios.md](Docs/DDD/TestEntitiesAndScenarios.md) |
| **Приклади JSON body** (швидке копіювання) | [Docs/ControllerModels/README.md](Docs/ControllerModels/README.md) |

Нижче збережено **довідник полів DTO** та узагальнений опис маршрутів; деталі по кожному методу підтримуйте в `Docs/Endpoints` (щоб не дублювати тричі).

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
| `companyMemberships` | масив `UserCompanyMembershipDto` | Для `GET /users/me` — компанії, де користувач є активним членом (не soft-deleted компанія). Для `GET /users` та `GET /users/search` — порожній масив. |

**`UserCompanyMembershipDto`** — елементи `companyMemberships` у `GET /users/me`

| Поле | Тип | Опис |
|------|-----|------|
| `companyId` | guid | Id компанії. |
| `companyName` | string | Назва. |
| `companySlug` | string | Slug для URL. |
| `membershipRole` | string | Роль у компанії у нижньому регістрі: `owner`, `manager`, `seller`, `support`, `logistics`. |
| `isOwner` | boolean | Чи позначено власника. |

### Каталог (компанії/категорії)

**`CompanyAddressRequest`** — вкладений об’єкт у `POST /admin/companies`, `PUT /admin/companies/{id}`

| Поле | Тип | Опис |
|------|-----|------|
| `street` | string | Вулиця/будинок. |
| `city` | string | Місто. |
| `state` | string | Область/регіон. |
| `postalCode` | string | Поштовий індекс. |
| `country` | string | Країна. |

**`CreateCompanyRequest`** — `POST /admin/companies`

| Поле | Тип | Опис |
|------|-----|------|
| `name` | string | Назва компанії. |
| `slug` | string | URL-ідентифікатор (унікальний). |
| `description` | string | Опис компанії. |
| `imageUrl` | string \| null | URL логотипа/банера. |
| `contactEmail` | string | Контактний email. |
| `contactPhone` | string | Контактний телефон. |
| `address` | `CompanyAddressRequest` | Адреса компанії. |
| `metaRaw` | string \| null | Додаткові JSON-дані (raw). |

**`UpdateCompanyRequest`** — `PUT /admin/companies/{id}`

| Поле | Тип | Опис |
|------|-----|------|
| `name` | string | Назва компанії. |
| `slug` | string | URL-ідентифікатор. |
| `description` | string | Опис компанії. |
| `imageUrl` | string \| null | URL зображення. |
| `contactEmail` | string | Контактний email. |
| `contactPhone` | string | Контактний телефон. |
| `address` | `CompanyAddressRequest` | Адреса компанії. |
| `metaRaw` | string \| null | Додаткові JSON-дані (raw). |

**`CompanyDto`** — елемент списків `GET /catalog/companies`, `GET /catalog/companies/{idOrSlug}`, `GET /admin/companies`, `GET /admin/companies/pending`; відповідь `GET /admin/companies/{id}`; також відповідь create/update компанії

| Поле | Тип | Опис |
|------|-----|------|
| `id` | guid | Id компанії. Генерується автоматично на бекенді під час створення. |
| `name` | string | Назва. |
| `slug` | string | URL-ідентифікатор. |
| `description` | string | Опис. |
| `imageUrl` | string \| null | URL зображення. |
| `contactEmail` | string | Контактний email. |
| `contactPhone` | string | Контактний телефон. |
| `address` | `CompanyAddressDto` | Адреса компанії. |
| `isApproved` | boolean | Чи підтверджена адміном. |
| `approvedAt` | string \| null | Коли підтверджено. |
| `approvedByUserId` | string \| null | Хто підтвердив. |
| `rating` | number \| null | Середній рейтинг. |
| `reviewCount` | int | К-сть відгуків. |
| `followerCount` | int | К-сть підписників. |
| `metaRaw` | string \| null | Додаткові JSON-дані (raw). |
| `createdAt` | string | Час створення. |
| `updatedAt` | string | Час оновлення. |
| `isDeleted` | boolean | Soft-delete прапор. |
| `deletedAt` | string \| null | Час soft-delete. |

**`CreateCategoryRequest`** — `POST /admin/categories`

| Поле | Тип | Опис |
|------|-----|------|
| `name` | string | Назва категорії. |
| `slug` | string | URL-ідентифікатор (унікальний). |
| `imageUrl` | string \| null | URL іконки/банера. |
| `parentCategoryId` | long \| null | Батьківська категорія. |
| `description` | string \| null | Опис категорії. |
| `metaRaw` | string \| null | Додаткові JSON-дані (raw). |
| `sortOrder` | int | Порядок сортування. |
| `isActive` | boolean | Активність категорії. |

**`UpdateCategoryRequest`** — `PUT /admin/categories/{id}`

| Поле | Тип | Опис |
|------|-----|------|
| `name` | string | Назва категорії. |
| `slug` | string | URL-ідентифікатор. |
| `imageUrl` | string \| null | URL іконки/банера. |
| `parentCategoryId` | long \| null | Батьківська категорія. |
| `description` | string \| null | Опис категорії. |
| `metaRaw` | string \| null | Додаткові JSON-дані (raw). |
| `sortOrder` | int | Порядок сортування. |

**`CategoryDto`** — елемент списків `GET /catalog/categories`, `GET /admin/categories`, `GET /admin/categories/active`; також відповідь create/update категорії

| Поле | Тип | Опис |
|------|-----|------|
| `id` | long | Id категорії. Генерується БД (auto-increment). |
| `name` | string | Назва. |
| `slug` | string | URL-ідентифікатор. |
| `imageUrl` | string \| null | URL зображення. |
| `parentId` | long \| null | Батьківська категорія. |
| `description` | string \| null | Опис. |
| `metaRaw` | string \| null | Додаткові JSON-дані (raw). |
| `sortOrder` | int | Порядок сортування. |
| `isActive` | boolean | Активність. |
| `productCount` | int | Агрегований лічильник (не зберігається в операційній таблиці категорій). |
| `createdAt` | string | Час створення. |
| `updatedAt` | string | Час оновлення. |
| `isDeleted` | boolean | Soft-delete прапор. |
| `deletedAt` | string \| null | Час soft-delete. |

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

#### `GET /account/2fa/status`

- **Що робить:** повертає поточний стан 2FA для акаунта (через `IAuthenticationPort`).
- **Авторизація:** **Bearer**.
- **Успіх (200):** `TwoFactorStatusDto`.

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

- **Що робить:** повертає профіль маркетплейсу для поточного Identity id і список компаній, у яких користувач має активне членство (`companyMemberships`).
- **Коли використовувати:** сторінка «мій профіль», ініціалізація стану після логіну, перемикач «моя компанія».
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

#### `PATCH /users/{id}/role`

- **Що робить:** змінює **глобальну** роль маркетплейсу (`UserRole`: buyer/seller/moderator/admin).
- **Авторизація:** **Bearer** + роль **`Admin`** (`[Authorize(Roles = "Admin")]`).
- **Path:** `id` — guid користувача.
- **Body:** `{ "role": "buyer" | "seller" | "moderator" | "admin" }` (без урахування регістру).
- **Успіх (200):** **порожнє тіло** при успіху.
- **Помилки:** **400** якщо `role` не парситься в enum.

> **Примітка:** `GET /users` та `GET /users/search` у коді доступні **будь-якому** автентифікованому користувачу (немає `Admin`-політики на контролері) — варто враховувати з точки зору безпеки продукту.

---

### `CompanyMembersController` — префікс `/companies/{companyId}/members`

Усі маршрути вимагають **Bearer**.  
Керування ролями доступне `owner`/`manager` у межах компанії або глобальному `Admin`.

#### `GET /companies/{companyId}/members`

- **Що робить:** повертає список членів компанії з ролями.
- **Path:** `companyId` — guid.
- **Успіх (200):** масив `CompanyMemberDto`.

#### `GET /companies/{companyId}/members/me`

- **Що робить:** повертає роль поточного користувача в конкретній компанії.
- **Path:** `companyId` — guid.
- **Успіх (200):** `CompanyMemberDto`.
- **Помилка:** якщо користувач не член компанії — повідомлення на кшталт `Membership not found` → зазвичай **404** (`ResultExtensions`: текст містить `not found`).

#### `POST /companies/{companyId}/members/{userId}/role`

- **Що робить:** призначає роль у компанії (upsert членства).
- **Path:** `companyId`, `userId` — guid.
- **Body:** `{ \"role\": \"owner|manager|seller|support|logistics\" }`.
- **Успіх (200):** `CompanyMemberDto`.

#### `PATCH /companies/{companyId}/members/{userId}/role`

- **Що робить:** змінює роль існуючого члена компанії.
- **Path:** `companyId`, `userId` — guid.
- **Body:** `{ \"role\": \"owner|manager|seller|support|logistics\" }`.
- **Успіх (200):** `CompanyMemberDto`.

#### `DELETE /companies/{companyId}/members/{userId}`

- **Що робить:** видаляє членство (soft-delete).
- **Path:** `companyId`, `userId` — guid.
- **Обмеження:** не можна видалити або понизити останнього `owner`.
- **Успіх (200):** **порожнє тіло**.

#### Future-proof до multi-role

- Поточний API працює у режимі **single-role per company-user**.
- Щоб перейти на multi-role без лому API, `POST/PATCH` можна інтерпретувати як `grant primary role`, а додаткові ролі винести в окремі маршрути `grant/revoke`.
- DB-шлях міграції: винести `role` у окрему M:N таблицю `company_member_roles` і залишити `company_members` як базовий зв'язок користувача з компанією.

---

### `InventoryController` — префікс `/companies/{companyId}` (внутрішній inventory)

Усі маршрути вимагають **Bearer**.  
Запис у склад (`receive/ship/adjust/transfer/reserve/release`, create/update/deactivate warehouse) дозволено ролям `owner|manager|logistics` у межах компанії або глобальному `Admin`.

#### `GET /companies/{companyId}/warehouses`
- Повертає склади компанії.

#### `POST /companies/{companyId}/warehouses`
- Створює склад.
- Body: `CreateWarehouseRequest`.

#### `PUT /companies/{companyId}/warehouses/{warehouseId}`
- Оновлює склад.
- Body: `CreateWarehouseRequest`.

#### `POST /companies/{companyId}/warehouses/{warehouseId}/deactivate`
- Деактивує склад.

#### `GET /companies/{companyId}/inventory/stocks?warehouseId=&productId=`
- Повертає залишки по складах з фільтрами.

#### `GET /companies/{companyId}/inventory/movements?productId=`
- Повертає журнал рухів стоку.

#### `POST /companies/{companyId}/inventory/receive`
- Оприбуткування.
- Body: `StockOperationRequest`.

#### `POST /companies/{companyId}/inventory/ship`
- Списання/відвантаження.
- Body: `StockOperationRequest`.

#### `POST /companies/{companyId}/inventory/adjust`
- Ручна корекція залишку.
- Body: `AdjustStockRequest`.

#### `POST /companies/{companyId}/inventory/transfer`
- Переміщення між складами.
- Body: `TransferStockRequest`.

#### `POST /companies/{companyId}/inventory/reservations`
- Створює резерв.
- Body: `ReserveStockRequest`.

#### `DELETE /companies/{companyId}/inventory/reservations/{reservationCode}`
- Знімає резерв.

---

### `CatalogController` — префікс `/catalog` (публічний)

#### `GET /catalog/companies`

- **Що робить:** повертає лише **підтверджені** компанії для публічного каталогу (без soft-deleted).
- **Авторизація:** не потрібна (`AllowAnonymous`).
- **Body:** немає.
- **Що повертає:** масив `CompanyDto` (може бути порожнім).

#### `GET /catalog/companies/{idOrSlug}`

- **Що робить:** картка **схваленої** компанії за **guid** або за **slug** (якщо рядок успішно парситься як `guid`, шукаємо за id).
- **Авторизація:** не потрібна (`AllowAnonymous`).
- **Що повертає:** один `CompanyDto`.
- **Помилки:** **404** якщо компанія не знайдена, не схвалена або soft-deleted.

#### `GET /catalog/categories`

- **Що робить:** повертає лише **активні** категорії для публічного каталогу.
- **Авторизація:** не потрібна (`AllowAnonymous`).
- **Body:** немає.
- **Що повертає:** масив `CategoryDto` (може бути порожнім).

#### `GET /catalog/categories/{id}`

- **Що робить:** деталь **активної** категорії за числовим id (не soft-deleted).
- **Авторизація:** не потрібна (`AllowAnonymous`).
- **Що повертає:** один `CategoryDto`.
- **Помилки:** **404** якщо категорія не знайдена, неактивна або видалена.

#### `GET /catalog/companies/{companyId}/products/{productId}/availability`

- **Що робить:** повертає агреговану наявність товару для storefront.
- **Що повертає:** `ProductAvailabilityDto` з `availableQty` та `availabilityStatus` (`in_stock`, `low_stock`, `out_of_stock`).

#### `GET /catalog/products`

- **Що робить:** повертає публічний список активних товарів без адмін-апруву товару.
- **Авторизація:** не потрібна (`AllowAnonymous`).
- **Що повертає:** масив `ProductListItemDto` з `availableQty` та `availabilityStatus`, де наявність агрегується із складів.

#### `GET /catalog/products/{slug}`

- **Що робить:** повертає деталку товару за `slug` без флоу `approve/revoke`.
- **Авторизація:** не потрібна (`AllowAnonymous`).
- **Що повертає:** `ProductDto` (`product`, `detail`, `images`) + наявність зі складської підсистеми.

---

### `ProductsController` — префікс `/companies/{companyId}/products` (внутрішній)

Усі маршрути вимагають **Bearer**.  
Матриця доступу:
- `Owner|Manager|Seller|Admin` — create/update/delete.
- `Support|Logistics` — read-only для внутрішнього списку.

#### `GET /companies/{companyId}/products`
- Повертає внутрішній список товарів компанії з availability.

#### `POST /companies/{companyId}/products`
- Створює товар без адмін-підтвердження.
- Body: `UpsertProductRequest`.

#### `PUT /companies/{companyId}/products/{id}`
- Оновлює товар.
- Body: `UpsertProductRequest`.

#### `DELETE /companies/{companyId}/products/{id}`
- Soft-delete товару.

---

### `AdminCatalogController` — префікс `/admin` (адмін)

Усі маршрути вимагають **Bearer** + роль **`Admin`**.

#### `GET /admin/companies`

- **Що робить:** повертає повний список компаній (у т.ч. непідтверджені).
- **Що повертає:** масив `CompanyDto`.

#### `GET /admin/companies/pending`

- **Що робить:** повертає лише компанії, які очікують модерації (`isApproved = false`).
- **Що повертає:** масив `CompanyDto`.

#### `GET /admin/companies/{id}`

- **Що робить:** повертає компанію за id (у т.ч. не схвалені та soft-deleted — як у БД).
- **Path:** `id` — guid.
- **Що повертає:** один `CompanyDto`.
- **Помилки:** **404** якщо запису немає.

#### `POST /admin/companies`

- **Що робить:** створює компанію (id генерується автоматично як `Guid`).
- **Body:** `CreateCompanyRequest`.
- **Що повертає:** один `CompanyDto` створеної компанії.

#### `PUT /admin/companies/{id}`

- **Що робить:** оновлює компанію за id.
- **Path:** `id` — guid.
- **Body:** `UpdateCompanyRequest`.
- **Що повертає:** оновлений `CompanyDto`.

#### `POST /admin/companies/{id}/approve`

- **Що робить:** підтверджує компанію від імені поточного адміна.
- **Path:** `id` — guid.
- **Що повертає:** **порожнє тіло** (`200 OK`) при успіху.

#### `POST /admin/companies/{id}/revoke-approval`

- **Що робить:** скасовує підтвердження компанії.
- **Path:** `id` — guid.
- **Що повертає:** **порожнє тіло** (`200 OK`) при успіху.

#### `DELETE /admin/companies/{id}`

- **Що робить:** soft-delete компанії.
- **Path:** `id` — guid.
- **Що повертає:** **порожнє тіло** (`200 OK`) при успіху.

#### `GET /admin/categories`

- **Що робить:** повертає всі категорії.
- **Що повертає:** масив `CategoryDto`.

#### `GET /admin/categories/active`

- **Що робить:** повертає лише активні категорії.
- **Що повертає:** масив `CategoryDto`.

#### `GET /admin/categories/{id}`

- **Що робить:** повертає категорію за id (у т.ч. неактивні та soft-deleted — як у БД).
- **Path:** `id` — long.
- **Що повертає:** один `CategoryDto`.
- **Помилки:** **404** якщо запису немає.

#### `POST /admin/categories`

- **Що робить:** створює категорію (id генерується БД auto-increment).
- **Body:** `CreateCategoryRequest`.
- **Що повертає:** один `CategoryDto` створеної категорії.

#### `PUT /admin/categories/{id}`

- **Що робить:** оновлює категорію за id.
- **Path:** `id` — long.
- **Body:** `UpdateCategoryRequest`.
- **Що повертає:** оновлений `CategoryDto`.

#### `POST /admin/categories/{id}/activate`

- **Що робить:** активує категорію.
- **Path:** `id` — long.
- **Що повертає:** **порожнє тіло** (`200 OK`) при успіху.

#### `POST /admin/categories/{id}/deactivate`

- **Що робить:** деактивує категорію.
- **Path:** `id` — long.
- **Що повертає:** **порожнє тіло** (`200 OK`) при успіху.

#### `DELETE /admin/categories/{id}`

- **Що робить:** soft-delete категорії.
- **Path:** `id` — long.
- **Що повертає:** **порожнє тіло** (`200 OK`) при успіху.

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

Деталізований опис кожного маршруту (side effects, кеш, ідемпотентність) див. у **[Docs/Endpoints/README.md](Docs/Endpoints/README.md)**.

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

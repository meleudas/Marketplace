# AuthController — `/auth`

## `POST /auth/register`

- **Summary (1 рядок):** Реєстрація нового користувача з підтвердженням email.
- **Призначення:** реєстрація в Identity + профіль маркетплейсу; лист з підтвердженням email.
- **Хто може викликати:**
  - JWT: не потрібна
  - Глобальні ролі: —
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Валідувати `RegisterRequest` (email, password, userName)
  2. Створити користувача в Identity та доменний профіль
  3. Надіслати лист підтвердження email
  4. Видати токени або повернути успіх без токенів (якщо `RequireConfirmedEmail`)
- **Side effects (синхронно):** створення користувача, email підтвердження; можлива установка refresh-cookie
- **Async / «магія»:** email-підтвердження через outbox/notification channel
- **Де на фронті:**
  - Екран: RegisterForm
  - API-модуль: `frontend/src/features/auth/api/auth.api.ts`
  - Статус: `implemented`
- **Приймає:** body `RegisterRequest` (`email`, `password`, `userName`, `phoneNumber?`).
- **Повертає:** при успіху з токенами — `AuthTokensDto` + Set-Cookie refresh; якщо увімкнено `RequireConfirmedEmail` — зазвичай **без** токенів (див. handler).
- **Помилки:** валідація, дубль email, **403** якщо політика вимагає підтвердження email перед видачею токенів.
- **Примітки:** деталі сценарію з email — у [Account.md](Account.md) та кореневому README.

## `POST /auth/login`

- **Summary (1 рядок):** Вхід за email і паролем з підтримкою 2FA.
- **Призначення:** вхід за email/паролем; оновлення `lastLogin` у доменному профілі; 2FA за потреби.
- **Хто може викликати:**
  - JWT: не потрібна
  - Глобальні ролі: —
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Перевірити облікові дані в Identity
  2. Якщо увімкнено 2FA — вимагати `twoFactorCode`
  3. Оновити `lastLogin` у профілі маркетплейсу
  4. Видати access/refresh токени та refresh-cookie
- **Side effects (синхронно):** нова пара access/refresh, cookie
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: LoginForm
  - API-модуль: `frontend/src/features/auth/api/auth.api.ts`
  - Статус: `implemented`
- **Приймає:** body `LoginRequest` (`email`, `password`, `rememberMe?`, `twoFactorCode?`).
- **Повертає:** **200** `AuthTokensDto` + refresh-cookie.
- **Помилки:** **401** невірі облікові дані; **401** з текстом про необхідність 2FA; **403** непідтверджений email.

## `POST /auth/refresh`

- **Summary (1 рядок):** Оновлення access-токена за валідним refresh.
- **Призначення:** оновлення access за валідним refresh; ротація refresh.
- **Хто може викликати:**
  - JWT: не потрібна (refresh з body або cookie)
  - Глобальні ролі: —
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Отримати refresh з body або cookie
  2. Перевірити валідність та термін дії
  3. Ревокнути старий refresh і видати нову пару токенів
- **Side effects (синхронно):** ревок старого refresh, запис нового
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: HTTP interceptor (автоматично)
  - API-модуль: `frontend/src/shared/api/http.client.ts`
  - Статус: `implemented`
- **Приймає:** body `RefreshRequest` (`refreshToken?`) або порожньо — тоді refresh з cookie (через middleware).
- **Повертає:** **200** `AuthTokensDto` + оновлений refresh-cookie.
- **Помилки:** невалідний/прострочений refresh; **403** якщо email не підтверджено (за політикою).

## `POST /auth/logout`

- **Summary (1 рядок):** Вихід із сесією — ревок refresh-токенів.
- **Призначення:** ревок активних refresh для поточного користувача; видалення refresh-cookie.
- **Хто може викликати:**
  - JWT: обов'язково (Bearer access)
  - Глобальні ролі: будь-який автентифікований користувач
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Визначити `userId` з JWT claims
  2. Ревокнути всі активні refresh-токени користувача
  3. Видалити refresh-cookie
- **Side effects (синхронно):** refresh-токени в БД більше не приймаються для цього user
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Settings / header
  - API-модуль: `frontend/src/features/auth/api/auth.api.ts`
  - Статус: `implemented`
- **Приймає:** немає.
- **Повертає:** **200** порожнє тіло.
- **Помилки:** **401** без user id у claims.

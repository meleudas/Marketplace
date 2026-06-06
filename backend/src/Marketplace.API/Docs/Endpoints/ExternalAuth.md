# ExternalAuthController — Google OAuth (`/auth`)

## `GET /auth/google`

- **Summary (1 рядок):** Старт OAuth-авторизації через Google.
- **Призначення:** старт OAuth Google (`Challenge`); зберігає `returnPath` у внутрішньому state.
- **Хто може викликати:**
  - JWT: не потрібна
  - Глобальні ролі: —
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Зберегти `returnPath` у OAuth state
  2. Перевірити наявність `GoogleAuth:ClientId`
  3. Редірект на Google OAuth
- **Side effects (синхронно):** створення OAuth state
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Google OAuth redirect
  - API-модуль: `frontend/src/features/auth/api/auth.api.ts` (`buildGoogleAuthUrl`)
  - Статус: `implemented`
- **Приймає:** query `returnPath?` (за замовчуванням `"/auth/callback"`; **має починатися з `/`**).
- **Повертає:** **302** на Google; **503** якщо не налаштовано `GoogleAuth:ClientId`.
- **Помилки:** **500** якщо не вдалося зібрати callback URL.

## `GET /auth/google/return`

- **Summary (1 рядок):** Callback після Google — provision користувача та редірект на фронт.
- **Призначення:** callback після Google; створення/оновлення локального користувача; одноразовий exchange `code`; редірект на фронт.
- **Хто може викликати:**
  - JWT: не потрібна (зовнішня OAuth-схема)
  - Глобальні ролі: —
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Валідувати `appState` і зовнішню автентифікацію Google
  2. Створити або оновити локального користувача (provision)
  3. Згенерувати одноразовий exchange `code`
  4. Редірект на `{Frontend:BaseUrl}{returnPath}?code=...`
- **Side effects (синхронно):** sign-in / provision, sign-out external cookie
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: `/auth/callback` (обробка `code` query)
  - API-модуль: —
  - Статус: `partial` (бекенд-редирект; обмін code — окремий endpoint)
- **Приймає:** query `appState` (обов'язковий).
- **Повертає:** **302** на `{Frontend:BaseUrl}{returnPath}?code=...`.
- **Помилки:** **400** відсутній/невалідний state; **401** зовнішня автентифікація не вдалась; **500** не задано `Frontend:BaseUrl`.

## `POST /auth/google/callback`

- **Summary (1 рядок):** Обмін одноразового OAuth `code` на access JWT.
- **Призначення:** обмін короткоживучого `code` на access JWT; refresh лише в cookie.
- **Хто може викликати:**
  - JWT: не потрібна
  - Глобальні ролі: —
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Валідувати одноразовий `code`
  2. Видати access JWT та refresh у cookie
  3. Інвалідувати `code` (одноразовий)
- **Side effects (синхронно):** Set-Cookie refresh
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: `/auth/callback`
  - API-модуль: `frontend/src/features/auth/api/auth.api.ts` (`exchangeGoogleCallback`)
  - Статус: `implemented`
- **Приймає:** body `GoogleCallbackExchangeRequest` (`code`).
- **Повертає:** **200** `{ accessToken, accessTokenExpiresAt }` (camelCase); **без** `refreshToken` у JSON.
- **Помилки:** **400** без `code`; **401** прострочений/невалідний code.

## Примітка (інструментація)

У коді контролера можуть бути тимчасові debug-логи у локальний файл — для продакшену варто прибрати або вимкнути.

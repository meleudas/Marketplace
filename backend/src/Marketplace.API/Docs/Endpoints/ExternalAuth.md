# ExternalAuthController — Google OAuth (`/auth`)

## `GET /auth/google`

- **Призначення:** старт OAuth Google (`Challenge`); зберігає `returnPath` у внутрішньому state.
- **Приймає:** query `returnPath?` (за замовчуванням `"/auth/callback"`; **має починатися з `/`**).
- **Повертає:** **302** на Google; **503** якщо не налаштовано `GoogleAuth:ClientId`.
- **Авторизація:** не потрібна.
- **Side effects:** створення OAuth state.
- **Помилки:** **500** якщо не вдалося зібрати callback URL.

## `GET /auth/google/return`

- **Призначення:** callback після Google; створення/оновлення локального користувача; одноразовий exchange `code`; редірект на фронт.
- **Приймає:** query `appState` (обов’язковий).
- **Повертає:** **302** на `{Frontend:BaseUrl}{returnPath}?code=...`.
- **Авторизація:** не потрібна (зовнішня схема).
- **Side effects:** sign-in / provision, sign-out external cookie.
- **Помилки:** **400** відсутній/невалідний state; **401** зовнішня автентифікація не вдалась; **500** не задано `Frontend:BaseUrl`.

## `POST /auth/google/callback`

- **Призначення:** обмін короткоживучого `code` на access JWT; refresh лише в cookie.
- **Приймає:** body `GoogleCallbackExchangeRequest` (`code`).
- **Повертає:** **200** `{ accessToken, accessTokenExpiresAt }` (camelCase); **без** `refreshToken` у JSON.
- **Авторизація:** не потрібна.
- **Side effects:** Set-Cookie refresh.
- **Помилки:** **400** без `code`; **401** прострочений/невалідний code.

## Примітка (інструментація)

У коді контролера можуть бути тимчасові debug-логи у локальний файл — для продакшену варто прибрати або вимкнути.

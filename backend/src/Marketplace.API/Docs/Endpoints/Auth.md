# AuthController — `/auth`

## `POST /auth/register`

- **Призначення:** реєстрація в Identity + профіль маркетплейсу; лист з підтвердженням email.
- **Приймає:** body `RegisterRequest` (`email`, `password`, `userName`, `phoneNumber?`).
- **Повертає:** при успіху з токенами — `AuthTokensDto` + Set-Cookie refresh; якщо увімкнено `RequireConfirmedEmail` — зазвичай **без** токенів (див. handler).
- **Авторизація:** не потрібна.
- **Side effects:** створення користувача, email підтвердження; можлива установка refresh-cookie.
- **Помилки:** валідація, дубль email, **403** якщо політика вимагає підтвердження email перед видачею токенів.
- **Примітки:** деталі сценарію з email — у [Account.md](Account.md) та кореневому README.

## `POST /auth/login`

- **Призначення:** вхід за email/паролем; оновлення `lastLogin` у доменному профілі; 2FA за потреби.
- **Приймає:** body `LoginRequest` (`email`, `password`, `rememberMe?`, `twoFactorCode?`).
- **Повертає:** **200** `AuthTokensDto` + refresh-cookie.
- **Авторизація:** не потрібна.
- **Side effects:** нова пара access/refresh, cookie.
- **Помилки:** **401** невірі облікові дані; **401** з текстом про необхідність 2FA; **403** непідтверджений email.

## `POST /auth/refresh`

- **Призначення:** оновлення access за валідним refresh; ротація refresh.
- **Приймає:** body `RefreshRequest` (`refreshToken?`) або порожньо — тоді refresh з cookie (через middleware).
- **Повертає:** **200** `AuthTokensDto` + оновлений refresh-cookie.
- **Авторизація:** не потрібна.
- **Side effects:** ревок старого refresh, запис нового.
- **Помилки:** невалідний/прострочений refresh; **403** якщо email не підтверджено (за політикою).

## `POST /auth/logout`

- **Призначення:** ревок активних refresh для поточного користувача; видалення refresh-cookie.
- **Приймає:** немає.
- **Повертає:** **200** порожнє тіло.
- **Авторизація:** **Bearer** (валідний access).
- **Side effects:** refresh-токени в БД більше не приймаються для цього user.
- **Помилки:** **401** без user id у claims.

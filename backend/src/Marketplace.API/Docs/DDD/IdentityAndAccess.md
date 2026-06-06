# Bounded context: Identity & Access

## Призначення

Автентифікація користувача (пароль, Google OAuth, refresh-cookie), підтвердження email, скидання пароля, 2FA (email/Telegram), а також **глобальна роль** користувача в маркетплейсі.

## Ключові сутності

- **Identity user** — ASP.NET Identity (email, пароль, `EmailConfirmed`, 2FA).
- **Marketplace User (профіль)** — доменний/застосунковий профіль (`UserDto`, `lastLogin`, `isVerified` тощо).
- **JWT access** — короткоживучий токен для `Authorization: Bearer`.
- **Refresh token** — довгоживучий, у БД + HTTP-only cookie.
- **UserRole** (глобальна) — `Buyer`, `Seller`, `Moderator`, `Admin`.

## Зв’язки з іншими контекстами

- **Companies:** `IdentityUserId` (guid) збігається з `userId` у членствах компанії та в HTTP як актор.
- **Admin catalog:** роль **Admin** відкриває `/admin/*` без додаткових перевірок компанії.

## HTTP API

Див. [Endpoints/Auth.md](../Endpoints/Auth.md), [Endpoints/ExternalAuth.md](../Endpoints/ExternalAuth.md), [Endpoints/Account.md](../Endpoints/Account.md), [Endpoints/Users.md](../Endpoints/Users.md), [Endpoints/PushNotifications.md](../Endpoints/PushNotifications.md) (Web Push підписки за JWT-користувачем).

## Важливі політики

- **`RequireConfirmedEmail`** — production policy за замовчуванням увімкнена (`Identity:RequireConfirmedEmail=true`), після реєстрації токени не видаються до `confirm-email`; логін блокується з повідомленням у `detail` (**403** за мапінгом).
- **Password policy / lockout** — мінімум 12 символів, обов'язкові upper+lower+digit+special, lockout після 5 невдалих спроб на 15 хв.
- **Refresh replay protection** — повторне використання вже-ротованого refresh token трактуємо як replay-атаку і примусово revoke активних сесій користувача.
- **Password reset session hygiene** — успішний reset пароля автоматично revoke всіх активних refresh сесій.
- **Мапінг статусів** — `ResultExtensions`: ключові слова в тексті помилки визначають 401/403/404.

## Production perimeter

- `/metrics` захищений `Authorize(Roles=Admin)`.
- `/hangfire` та `sendgrid/key-trace` доступні лише в `Development`.
- Telegram webhook у non-dev режимі вимагає заданий `WebhookSecret`.

## Тестовий контур домену

- Unit/application/API/integration/contract/security/performance сценарії покриті `Suite=IdentityAccess`.
- CI має окремий gate `identity-access-gate` з line coverage threshold 12%.

## Файли для орієнтації

- `Marketplace.API/Controllers/AuthController.cs`
- `Marketplace.API/Controllers/ExternalAuthController.cs`
- `Marketplace.API/Controllers/AccountController.cs`
- `Marketplace.API/Controllers/UsersController.cs`
- `Marketplace.API/Controllers/PushNotificationsController.cs`
- `Marketplace.Domain/Users/Enums/UserRole.cs`

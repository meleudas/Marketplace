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

Див. [Endpoints/Auth.md](../Endpoints/Auth.md), [Endpoints/ExternalAuth.md](../Endpoints/ExternalAuth.md), [Endpoints/Account.md](../Endpoints/Account.md), [Endpoints/Users.md](../Endpoints/Users.md).

## Важливі політики

- **`RequireConfirmedEmail`** — після реєстрації токени можуть не видаватись до `confirm-email`; логін блокується з повідомленням у `detail` (**403** за мапінгом).
- **Мапінг статусів** — `ResultExtensions`: ключові слова в тексті помилки визначають 401/403/404.

## Файли для орієнтації

- `Marketplace.API/Controllers/AuthController.cs`
- `Marketplace.API/Controllers/ExternalAuthController.cs`
- `Marketplace.API/Controllers/AccountController.cs`
- `Marketplace.API/Controllers/UsersController.cs`
- `Marketplace.Domain/Users/Enums/UserRole.cs`

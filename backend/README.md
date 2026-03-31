# Marketplace API

Backend for a marketplace platform built on .NET 10 with ASP.NET Core Web API, DDD/Hexagonal architecture, PostgreSQL, Redis, MongoDB, JWT, refresh tokens, Google OAuth2 for SPA flows, SendGrid email delivery, and email-based 2FA.

## Tech stack

- .NET 10, C#
- ASP.NET Core Web API
- EF Core + PostgreSQL (`Npgsql`)
- ASP.NET Identity
- Redis (cache/state)
- MongoDB (optional storage extensions)
- Docker / Docker Compose
- Scalar + Swagger UI (OpenAPI documentation)
- SendGrid (transactional email)

## Project structure

```text
src/
  Marketplace.Domain/
  Marketplace.Application/
  Marketplace.Infrastructure/
  Marketplace.API/
```

## Prerequisites

- .NET SDK 10
- Docker Desktop (or Docker Engine + Compose)
- Google Cloud OAuth credentials (for Google sign-in)

## Environment variables

Copy `.env.example` to `.env` and fill values:

```bash
cp .env.example .env
```

Required variables:

- `GOOGLEAUTH__CLIENTID`
- `GOOGLEAUTH__CLIENTSECRET`
- `SENDGRID__APIKEY`
- `SENDGRID__FROMEMAIL`
- `SENDGRID__FROMNAME` (optional)
- `TELEGRAM__BOTTOKEN` (required for Telegram 2FA)
- `TELEGRAM__WEBHOOKSECRET` (optional but recommended)
- `TELEGRAM__LINKCODETTLMINUTES` (optional, default `10`)
- `FRONTEND__BASEURL` (e.g. `http://localhost:3000`)
- `CORS__ALLOWEDORIGINS__0` (allowed SPA origin, usually same as `FRONTEND__BASEURL`)
- `JWT__SECRETKEY` (minimum 32 chars)

> `.env` is ignored by git. Keep secrets there only.

## Run with Docker (recommended)

```bash
docker compose up -d --build
```

Services:

- API: `http://localhost:8080`
- PostgreSQL: `localhost:5432`
- Redis: `localhost:6379`
- MongoDB: `localhost:27017`

Stop services:

```bash
docker compose down
```

Stop and remove volumes (full reset):

```bash
docker compose down -v
```

## Run locally (without Docker for API)

1. Start infrastructure containers only:

```bash
docker compose up -d postgres redis mongo
```

2. Run API:

```bash
dotnet run --project src/Marketplace.API/Marketplace.API.csproj
```

API applies migrations automatically in `Development`.

## API docs (Swagger + Scalar + OpenAPI)

- Swagger UI: `http://localhost:8080/swagger`
- Scalar UI: `http://localhost:8080/scalar`
- OpenAPI JSON: `http://localhost:8080/openapi/v1.json`

You can authorize in Swagger via **Authorize** button:

1. Login or register.
2. Copy `accessToken` from response.
3. Click **Authorize** and paste `Bearer <accessToken>`.
4. Call protected endpoints (e.g. `/users/me`).

## Auth overview

- Access token: JWT (default 10 min)
- Refresh token: HTTPOnly cookie (default 30 days)
- Email/password register & login
- Refresh endpoint
- Google OAuth2 flow for SPA (`/auth/google`, callback + code exchange)
- Email confirmation + password reset
- Email 2FA flow (send code, enable/disable, login challenge)

## Email and 2FA setup

### SendGrid

- Configure `SENDGRID__APIKEY` and verified sender in `SENDGRID__FROMEMAIL`.
- `SENDGRID__FROMNAME` is display name of the sender.
- If SendGrid is not configured, app falls back to logging email sender (development convenience).

### Email 2FA endpoints

- `POST /account/2fa/email/send-code` (authorized) - send one-time code to account email.
- `POST /account/2fa/email/enable` (authorized) - enable 2FA with body `{ "code": "123456" }`.
- `POST /account/2fa/email/disable` (authorized) - disable 2FA.

### Telegram 2FA endpoints

- `POST /account/2fa/telegram/link-code` (authorized) - generate one-time link code.
- Open Telegram bot and send `/start <link_code>` to bind chat.
- `POST /account/2fa/telegram/send-code` (authorized) - send one-time login code to Telegram.
- `POST /account/2fa/telegram/enable` (authorized) - enable Telegram 2FA with body `{ "code": "123456" }`.
- `POST /account/2fa/telegram/disable` (authorized) - disable Telegram 2FA.

Telegram webhook endpoint:

- `POST /integrations/telegram/webhook`
- Optional security header: `X-Telegram-Bot-Api-Secret-Token` must match `TELEGRAM__WEBHOOKSECRET`.

When 2FA is enabled, login requires `twoFactorCode` in request body:

```json
{
  "email": "user@example.com",
  "password": "StrongPassword123!",
  "rememberMe": false,
  "twoFactorCode": "123456"
}
```

## Google OAuth setup checklist

In Google Cloud Console:

1. Create OAuth 2.0 Client ID.
2. Add authorized redirect URI for backend callback (example):
   - `http://localhost:8080/auth/google/return`
3. Put `ClientId` and `ClientSecret` into `.env`.
4. Set `FRONTEND__BASEURL` to your SPA URL.

## Health check

- `GET /health`

## Useful commands

```bash
dotnet restore
dotnet build Marketplace.slnx
dotnet test
```

## Notes

- Current repository is optimized for development-first flow with Docker.
- Keep production secrets in secure secret management (not in repo).

# Marketplace API

Backend for a marketplace platform built on .NET 10 with ASP.NET Core Web API, DDD/Hexagonal architecture, PostgreSQL, Redis, MongoDB, JWT, refresh tokens, and Google OAuth2 for SPA flows.

## Tech stack

- .NET 10, C#
- ASP.NET Core Web API
- EF Core + PostgreSQL (`Npgsql`)
- ASP.NET Identity
- Redis (cache/state)
- MongoDB (optional storage extensions)
- Docker / Docker Compose
- Scalar (OpenAPI documentation UI)

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
- `FRONTEND__BASEURL` (e.g. `http://localhost:3000`)
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

## API docs (Scalar + OpenAPI)

- Scalar UI: `http://localhost:8080/scalar`
- OpenAPI JSON: `http://localhost:8080/openapi/v1.json`

## Auth overview

- Access token: JWT (default 10 min)
- Refresh token: HTTPOnly cookie (default 30 days)
- Email/password register & login
- Refresh endpoint
- Google OAuth2 flow for SPA (`/auth/google`, callback + code exchange)

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

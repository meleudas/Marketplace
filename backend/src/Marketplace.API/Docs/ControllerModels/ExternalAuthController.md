# ExternalAuthController

Контролер: `/auth/google*`

## Авторизація
- Усі endpoints - `AllowAnonymous`

## Моделі запитів (copy-paste)

### GET `/auth/google?returnPath=/auth/callback`
- Body не потрібен.

### GET `/auth/google/return?appState=...`
- Body не потрібен.

### POST `/auth/google/callback`
```json
{
  "code": "google_exchange_code_here"
}
```

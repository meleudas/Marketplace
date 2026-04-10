# AuthController

Контролер: `POST /auth/*`

## Авторизація
- `POST /auth/register` - `AllowAnonymous`
- `POST /auth/login` - `AllowAnonymous`
- `POST /auth/refresh` - `AllowAnonymous`
- `POST /auth/logout` - `Authorize` (будь-який валідний JWT)

## Моделі запитів (copy-paste)

### POST `/auth/register`
```json
{
  "email": "user@example.com",
  "password": "Admin123!",
  "userName": "Test User",
  "phoneNumber": "+380991112233"
}
```

### POST `/auth/login`
```json
{
  "email": "user@example.com",
  "password": "Admin123!",
  "rememberMe": false,
  "twoFactorCode": null
}
```

### POST `/auth/refresh`
```json
{
  "refreshToken": null
}
```

### POST `/auth/logout`
- Body не потрібен.

# UsersController

Контролер: `/users/*`

## Авторизація
- Базово: `Authorize` на весь контролер
- `PATCH /users/{id}/role` - додатково роль `Admin`

## Моделі запитів (copy-paste)

### GET `/users/me`
- Body не потрібен.

### GET `/users`
- Body не потрібен.

### GET `/users/search?userName=test`
- Body не потрібен.

### DELETE `/users/{id}`
- Body не потрібен.

### PATCH `/users/{id}/role` (Admin only)
```json
{
  "role": "Seller"
}
```

Дозволені значення `role`: `Buyer`, `Seller`, `Moderator`, `Admin`.

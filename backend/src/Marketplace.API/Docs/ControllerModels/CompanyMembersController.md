# CompanyMembersController

Контролер: `/companies/{companyId}/members/*`

## Авторизація
- Увесь контролер: `Authorize`
- Фактичний доступ усередині handler-ів:
  - `Admin` або company-role (`Owner`/`Manager`) для керування ролями

## Моделі запитів (copy-paste)

### GET `/companies/{companyId}/members`
- Body не потрібен.

### GET `/companies/{companyId}/members/me`
- Body не потрібен.

### POST `/companies/{companyId}/members/{userId}/role`
```json
{
  "role": "Seller"
}
```

### PATCH `/companies/{companyId}/members/{userId}/role`
```json
{
  "role": "Logistics"
}
```

### DELETE `/companies/{companyId}/members/{userId}`
- Body не потрібен.

Дозволені значення `role`: `Owner`, `Manager`, `Seller`, `Support`, `Logistics`.

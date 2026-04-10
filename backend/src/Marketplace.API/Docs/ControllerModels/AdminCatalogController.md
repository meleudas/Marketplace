# AdminCatalogController

Контролер: `/admin/*`

## Авторизація
- Увесь контролер: `Authorize(Roles = "Admin")`

## Моделі запитів (copy-paste)

### POST `/admin/companies`
```json
{
  "name": "My Company",
  "slug": "my-company",
  "description": "Company description",
  "imageUrl": "https://example.com/logo.png",
  "contactEmail": "company@example.com",
  "contactPhone": "+380991112233",
  "address": {
    "street": "Main st 1",
    "city": "Kyiv",
    "state": "Kyiv",
    "postalCode": "01001",
    "country": "UA"
  },
  "metaRaw": "{\"source\":\"admin\"}"
}
```

### PUT `/admin/companies/{id}`
```json
{
  "name": "My Company Updated",
  "slug": "my-company",
  "description": "Updated description",
  "imageUrl": "https://example.com/logo2.png",
  "contactEmail": "company@example.com",
  "contactPhone": "+380991112233",
  "address": {
    "street": "Main st 2",
    "city": "Kyiv",
    "state": "Kyiv",
    "postalCode": "01001",
    "country": "UA"
  },
  "metaRaw": "{\"source\":\"admin-update\"}"
}
```

### POST `/admin/categories`
```json
{
  "name": "Electronics",
  "slug": "electronics",
  "imageUrl": "https://example.com/cat.png",
  "parentCategoryId": null,
  "description": "Category description",
  "metaRaw": "{\"showOnMain\":true}",
  "sortOrder": 0,
  "isActive": true
}
```

### PUT `/admin/categories/{id}`
```json
{
  "name": "Electronics Updated",
  "slug": "electronics",
  "imageUrl": "https://example.com/cat2.png",
  "parentCategoryId": null,
  "description": "Updated category",
  "metaRaw": "{\"showOnMain\":true}",
  "sortOrder": 1
}
```

### GET/POST approve/revoke/delete endpoints
- Body не потрібен:
  - `GET /admin/companies`
  - `GET /admin/companies/pending`
  - `POST /admin/companies/{id}/approve`
  - `POST /admin/companies/{id}/revoke-approval`
  - `DELETE /admin/companies/{id}`
  - `GET /admin/categories`
  - `GET /admin/categories/active`
  - `POST /admin/categories/{id}/activate`
  - `POST /admin/categories/{id}/deactivate`
  - `DELETE /admin/categories/{id}`

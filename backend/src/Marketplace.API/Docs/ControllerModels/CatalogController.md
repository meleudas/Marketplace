# CatalogController

Контролер: `/catalog/*`

## Авторизація
- Увесь контролер: `AllowAnonymous`

## Моделі запитів (copy-paste)

### GET `/catalog/companies`
- Body не потрібен.

### GET `/catalog/companies/{idOrSlug}`
- Body не потрібен. Сегмент — guid компанії або slug.

### GET `/catalog/categories`
- Body не потрібен.

### GET `/catalog/categories/{id}`
- Body не потрібен.

### GET `/catalog/companies/{companyId}/products/{productId}/availability`
- Body не потрібен.

### GET `/catalog/products`
- Body не потрібен.

### GET `/catalog/products/{slug}`
- Body не потрібен.

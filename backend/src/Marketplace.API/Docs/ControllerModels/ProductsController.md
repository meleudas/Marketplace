# ProductsController

Контролер: `/companies/{companyId}/products/*`

## Авторизація
- Увесь контролер: `Authorize`
- Запис (`POST/PUT/DELETE`): `Owner|Manager|Seller|Admin`
- Читання внутрішнього списку (`GET`): будь-яка роль компанії + `Admin`

## Моделі запитів (copy-paste)

### GET `/companies/{companyId}/products`
- Body не потрібен.

### POST `/companies/{companyId}/products`
```json
{
  "name": "Gaming Keyboard",
  "slug": "gaming-keyboard",
  "description": "Mechanical keyboard with RGB",
  "price": 3499.99,
  "oldPrice": 3999.99,
  "minStock": 5,
  "categoryId": 10,
  "hasVariants": true,
  "detail": {
    "slug": "gaming-keyboard",
    "attributesRaw": "{\"switch\":\"brown\"}",
    "variantsRaw": "{\"colors\":[\"black\",\"white\"]}",
    "specificationsRaw": "{\"layout\":\"TKL\"}",
    "seoRaw": "{\"title\":\"Gaming Keyboard\"}",
    "contentBlocksRaw": "{\"blocks\":[]}",
    "tags": ["keyboard", "gaming"],
    "brands": ["BrandX"]
  },
  "images": [
    {
      "imageUrl": "https://cdn.example.com/products/kb-main.jpg",
      "thumbnailUrl": "https://cdn.example.com/products/kb-main-thumb.jpg",
      "altText": "Gaming keyboard front view",
      "sortOrder": 0,
      "isMain": true,
      "width": 1200,
      "height": 800,
      "fileSize": 245000
    }
  ]
}
```

### PUT `/companies/{companyId}/products/{id}`
- Body такий самий як у `POST`.

### DELETE `/companies/{companyId}/products/{id}`
- Body не потрібен.

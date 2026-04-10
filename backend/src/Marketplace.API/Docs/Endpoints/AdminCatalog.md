# AdminCatalogController — `/admin`

Усі маршрути: **`[Authorize(Roles = "Admin")]`** + Bearer.

## Компанії

### `GET /admin/companies`

- **Призначення:** повний список компаній (у т.ч. не схвалені).
- **Повертає:** масив `CompanyDto`.
- **Side effects:** немає.

### `GET /admin/companies/pending`

- **Призначення:** компанії з `isApproved = false`.
- **Повертає:** масив `CompanyDto`.

### `POST /admin/companies`

- **Приймає:** body `CreateCompanyRequest` (див. кореневий README, таблиці полів).
- **Повертає:** **200** створений `CompanyDto` (`id` — новий `Guid`).
- **Side effects:** нова компанія в БД.

### `PUT /admin/companies/{id}`

- **Приймає:** path `id` (guid); body `UpdateCompanyRequest`.
- **Повертає:** оновлений `CompanyDto`.
- **Side effects:** оновлення запису компанії.

### `POST /admin/companies/{id}/approve`

- **Призначення:** схвалити компанію від імені поточного адміна.
- **Приймає:** path `id`; з JWT береться `adminUserId` для аудиту.
- **Повертає:** **200** порожнє тіло при успіху.
- **Side effects:** `isApproved`, `approvedAt`, `approvedByUserId`.

### `POST /admin/companies/{id}/revoke-approval`

- **Повертає:** **200** порожнє тіло.
- **Side effects:** скасування апруву (компанія зникає з публічного каталогу approved).

### `DELETE /admin/companies/{id}`

- **Повертає:** **200** порожнє тіло.
- **Side effects:** soft-delete компанії.

## Категорії

### `GET /admin/categories`

- **Повертає:** масив `CategoryDto` (усі).

### `GET /admin/categories/active`

- **Повертає:** масив `CategoryDto` лише активні.

### `POST /admin/categories`

- **Приймає:** body `CreateCategoryRequest`.
- **Повертає:** створений `CategoryDto` (`id` — auto-increment БД).
- **Side effects:** нова категорія; можливий вплив на публічний `GET /catalog/categories` (лише активні).

### `PUT /admin/categories/{id}`

- **Приймає:** path `id` (long); body `UpdateCategoryRequest`.
- **Повертає:** оновлений `CategoryDto`.

### `POST /admin/categories/{id}/activate`

- **Повертає:** **200** порожнє тіло.
- **Side effects:** категорія стає видимою в публічному каталозі (якщо не deleted).

### `POST /admin/categories/{id}/deactivate`

- **Повертає:** **200** порожнє тіло.
- **Side effects:** прибирається з публічного списку активних.

### `DELETE /admin/categories/{id}`

- **Повертає:** **200** порожнє тіло.
- **Side effects:** soft-delete категорії.

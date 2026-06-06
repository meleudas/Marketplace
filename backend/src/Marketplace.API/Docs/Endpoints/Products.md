# ProductsController — `/companies/{companyId}/products`

Усі маршрути: **`[Authorize]`** + Bearer.

### Матриця доступу (з `ProductAccessService`)

| Дія | Умова |
|-----|--------|
| Будь-який запит | Користувач — член компанії (`CompanyMember`) **або** глобальний **Admin** |
| Читання (`ReadInternal`) | Будь-який член компанії |
| Створення/оновлення/видалення (`WriteProduct`) | Роль члена: **Owner**, **Manager**, **Seller** |
| Admin | Bypass перевірки членства |

## `GET /companies/{companyId}/products`

- **Summary (1 рядок):** Внутрішній список товарів компанії для кабінету продавця.
- **Призначення:** внутрішній список товарів компанії з інформацією про наявність (для кабінету продавця).
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin** (bypass)
  - Компанійні ролі: будь-який член (`ReadInternal`)
- **Бізнес-логіка:**
  1. Перевірити членство або роль Admin
  2. Завантажити товари компанії з availability
  3. Повернути колекцію DTO
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: WorkspaceProductsScreen
  - API-модуль: `frontend/src/features/workspace/api/products.api.ts`
  - Статус: `implemented`
- **Приймає:** path `companyId` (guid).
- **Повертає:** колекція DTO з handler-а `GetCompanyProductsQuery` (внутрішній перегляд + availability).
- **Помилки:** **Forbidden** якщо не член і не Admin.

## `POST /companies/{companyId}/products`

- **Summary (1 рядок):** Створення товару з автоматичною подачею на модерацію.
- **Призначення:** створити товар компанії та автоматично подати його на модерацію (`PendingReview`).
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin** (bypass)
  - Компанійні ролі: **Owner**, **Manager**, **Seller** (`WriteProduct`)
- **Бізнес-логіка:**
  1. Перевірити `WriteProduct` доступ
  2. Створити продукт, деталі та зображення
  3. Встановити статус `PendingReview` та інвалідувати кеш каталогу
- **Side effects (синхронно):** запис продукту, деталей, зображень; інвалідація `catalog:products:list` та `catalog:products:detail:{slug}`
- **Async / «магія»:** in-app нотифікація адмінам/модераторам про товар на перевірку
- **Де на фронті:**
  - Екран: WorkspaceProductsScreen
  - API-модуль: `frontend/src/features/workspace/api/products.api.ts`
  - Статус: `implemented`
- **Приймає:** body `UpsertProductRequest` (`name`, `slug`, `description`, `price`, `oldPrice?`, `minStock`, `categoryId`, `hasVariants`, `detail?`, `images?`).
- **Повертає:** **200** `ProductDto` (створений товар з деталями/зображеннями за наявності).
- **Metrics:** `product_latency_ms{operation="products_create"}`, `product_operations_total{operation="products_create",status="success"}`, `product_errors_total{operation="products_create",reason=*}`.

## `PUT /companies/{companyId}/products/{id}`

- **Summary (1 рядок):** Оновлення товару з можливою повторною модерацією.
- **Призначення:** оновити товар компанії; при статусі `Draft` — повторна подача на модерацію.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin** (bypass)
  - Компанійні ролі: **Owner**, **Manager**, **Seller** (`WriteProduct`)
- **Бізнес-логіка:**
  1. Перевірити `WriteProduct` доступ
  2. Оновити сутність продукту
  3. Якщо був `Draft` — встановити `PendingReview`; інвалідувати кеш для старого та нового slug
- **Side effects (синхронно):** оновлення сутності; інвалідація кешу для старого та нового `slug` + список
- **Async / «магія»:** нотифікація черги модерації (якщо повторна подача)
- **Де на фронті:**
  - Екран: WorkspaceProductsScreen (edit)
  - API-модуль: `frontend/src/features/workspace/api/products.api.ts`
  - Статус: `partial`
- **Приймає:** path `companyId`, `id` (long); body `UpsertProductRequest`.
- **Повертає:** **200** оновлений `ProductDto`.
- **Metrics:** `product_latency_ms{operation="products_update"}`, `product_operations_total{operation="products_update",status="success"}`, `product_errors_total{operation="products_update",reason=*}`.

## `DELETE /companies/{companyId}/products/{id}`

- **Summary (1 рядок):** М'яке видалення товару компанії.
- **Призначення:** soft-delete товару.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin** (bypass)
  - Компанійні ролі: **Owner**, **Manager**, **Seller** (`WriteProduct`)
- **Бізнес-логіка:**
  1. Перевірити `WriteProduct` доступ
  2. Виконати soft-delete товару
  3. Інвалідувати кеш списку та деталі
- **Side effects (синхронно):** товар прихований з вітрини; інвалідація кешу списку та деталі за старим slug
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: WorkspaceProductsScreen (delete)
  - API-модуль: `frontend/src/features/workspace/api/products.api.ts`
  - Статус: `partial`
- **Повертає:** **200** порожнє тіло при успіху.
- **Metrics:** `product_latency_ms{operation="products_delete"}`, `product_operations_total{operation="products_delete",status="success"}`, `product_errors_total{operation="products_delete",reason=*}`.

## `POST /companies/{companyId}/products/{id}/images:upload`

- **Summary (1 рядок):** Завантаження зображення товару з async-обробкою.
- **Призначення:** завантажити оригінал зображення для товару з подальшою async-обробкою.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin** (bypass)
  - Компанійні ролі: **Owner**, **Manager**, **Seller** (`WriteProduct`)
- **Бізнес-логіка:**
  1. Перевірити `WriteProduct` та валідацію файлу (MIME, розмір)
  2. Завантажити оригінал у object storage
  3. Оновити набір зображень товару та поставити в чергу image-processing
- **Side effects (синхронно):** upload в object storage, перезапис набору зображень товару
- **Async / «магія»:** enqueue в image-processing dispatcher (resize/variants)
- **Де на фронті:**
  - Екран: WorkspaceProductsScreen (image upload, planned)
  - API-модуль: `frontend/src/features/workspace/api/products.api.ts`
  - Статус: `partial`
- **Валідація:** storage має бути увімкнений, файл обов'язковий, обмеження розміру, підтримувані MIME: `image/jpeg`, `image/png`, `image/webp`.
- **Metrics:** `product_latency_ms{operation="products_upload_image"}`, `product_operations_total{operation="products_upload_image",status="success"}`, `product_errors_total{operation="products_upload_image",reason="storage_disabled|file_missing|file_too_large|unsupported_type|unauthorized|forbidden|not_found|application_failure"}`.

## Типові помилки

- `Forbidden` — немає членства або недостатньо ролі для запису.
- `Product not found` — невірний `id` або чужий `companyId` (залежно від handler).
- Помилки валідації slug/категорії — згідно command validator-ів.

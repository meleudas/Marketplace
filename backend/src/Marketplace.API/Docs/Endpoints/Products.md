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

- **Призначення:** внутрішній список товарів компанії з інформацією про наявність (для кабінету продавця).
- **Приймає:** path `companyId` (guid).
- **Повертає:** колекція DTO з handler-а `GetCompanyProductsQuery` (внутрішній перегляд + availability).
- **Авторизація:** `ReadInternal` (будь-який член або Admin).
- **Помилки:** **Forbidden** якщо не член і не Admin.

## `POST /companies/{companyId}/products`

- **Призначення:** створити товар компанії та автоматично подати його на модерацію (`PendingReview`).
- **Приймає:** body `UpsertProductRequest` (`name`, `slug`, `description`, `price`, `oldPrice?`, `minStock`, `categoryId`, `hasVariants`, `detail?`, `images?`).
- **Повертає:** **200** `ProductDto` (створений товар з деталями/зображеннями за наявності).
- **Авторизація:** `WriteProduct`.
- **Side effects:** запис продукту, деталей, зображень; статус `PendingReview`; нотифікація адмінам/модераторам про товар на перевірку; **інвалідація кешу:** `catalog:products:list` та `catalog:products:detail:{slug}`.
- **Metrics:** `product_latency_ms{operation="products_create"}`, `product_operations_total{operation="products_create",status="success"}`, `product_errors_total{operation="products_create",reason=*}`.

## `PUT /companies/{companyId}/products/{id}`

- **Приймає:** path `companyId`, `id` (long); body `UpsertProductRequest`.
- **Повертає:** **200** оновлений `ProductDto`.
- **Авторизація:** `WriteProduct`.
- **Side effects:** оновлення сутності; якщо товар був у `Draft` — повторна подача на модерацію (`PendingReview`) + нотифікація черги модерації; **інвалідація кешу** для старого та нового `slug` + список.
- **Metrics:** `product_latency_ms{operation="products_update"}`, `product_operations_total{operation="products_update",status="success"}`, `product_errors_total{operation="products_update",reason=*}`.

## `DELETE /companies/{companyId}/products/{id}`

- **Призначення:** soft-delete товару.
- **Повертає:** **200** порожнє тіло при успіху.
- **Авторизація:** `WriteProduct`.
- **Side effects:** товар прихований з вітрини; **інвалідація кешу** списку та деталі за старим slug.
- **Metrics:** `product_latency_ms{operation="products_delete"}`, `product_operations_total{operation="products_delete",status="success"}`, `product_errors_total{operation="products_delete",reason=*}`.

## `POST /companies/{companyId}/products/{id}/images:upload`

- **Призначення:** завантажити оригінал зображення для товару з подальшою async-обробкою.
- **Авторизація:** `WriteProduct`.
- **Валідація:** storage має бути увімкнений, файл обов'язковий, обмеження розміру, підтримувані MIME: `image/jpeg`, `image/png`, `image/webp`.
- **Side effects:** upload в object storage, перезапис набору зображень товару, enqueue в image-processing dispatcher.
- **Metrics:** `product_latency_ms{operation="products_upload_image"}`, `product_operations_total{operation="products_upload_image",status="success"}`, `product_errors_total{operation="products_upload_image",reason="storage_disabled|file_missing|file_too_large|unsupported_type|unauthorized|forbidden|not_found|application_failure"}`.

## Типові помилки

- `Forbidden` — немає членства або недостатньо ролі для запису.
- `Product not found` — невірний `id` або чужий `companyId` (залежно від handler).
- Помилки валідації slug/категорії — згідно command validator-ів.

# CatalogController — `/catalog`

Усі маршрути: **`[AllowAnonymous]`** (публічний вітринний API).

## `GET /catalog/companies`

- **Summary (1 рядок):** Публічний список схвалених компаній.
- **Призначення:** публічний список **схвалених** компаній (без soft-deleted).
- **Хто може викликати:**
  - JWT: не потрібна
  - Глобальні ролі: —
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Завантажити схвалені компанії (read-through кеш)
  2. Відфільтрувати soft-deleted
  3. Повернути масив `CompanyDto`
- **Side effects (синхронно):** можливе читання з кешу (`catalog:companies:approved` — див. `CatalogCacheKeys` у застосунку)
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: CompaniesScreen
  - API-модуль: `frontend/src/features/storefront/api/catalog.api.ts`
  - Статус: `implemented`
- **Повертає:** масив `CompanyDto`.
- **Примітки:** не показує неапрувнуті компанії.

## `GET /catalog/companies/{idOrSlug}`

- **Summary (1 рядок):** Публічна картка компанії за id або slug.
- **Призначення:** публічна картка компанії за **guid** або **slug** (якщо сегмент парситься як `guid`, використовується пошук за id).
- **Хто може викликати:**
  - JWT: не потрібна
  - Глобальні ролі: —
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Визначити пошук за guid або slug
  2. Завантажити схвалену не deleted компанію (кеш TTL 10 хв)
  3. Повернути `CompanyDto` або 404
- **Side effects (синхронно):** read-through кеш
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Company detail (planned route)
  - API-модуль: `frontend/src/features/storefront/api/catalog.api.ts`
  - Статус: `partial`
- **Повертає:** **200** `CompanyDto` лише для **схваленої** та не soft-deleted компанії.
- **Кеш:** `catalog:companies:id:{guid}` або `catalog:companies:slug:{slug}`, TTL **10 хв**.
- **Помилки:** **404** `Company not found`.

## `GET /catalog/categories`

- **Summary (1 рядок):** Активні категорії для вітрини.
- **Призначення:** **активні** категорії для вітрини.
- **Хто може викликати:**
  - JWT: не потрібна
  - Глобальні ролі: —
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Завантажити активні категорії (read-through кеш)
  2. Повернути масив `CategoryDto`
- **Side effects (синхронно):** можливе читання з кешу (`catalog:categories:active`)
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: HomeScreen, ProductsScreen
  - API-модуль: `frontend/src/features/storefront/api/catalog.api.ts`
  - Статус: `implemented`
- **Повертає:** масив `CategoryDto`.

## `GET /catalog/categories/{id}`

- **Summary (1 рядок):** Публічна деталь активної категорії.
- **Призначення:** публічна деталь категорії за числовим id — лише **активна** та не soft-deleted.
- **Хто може викликати:**
  - JWT: не потрібна
  - Глобальні ролі: —
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти категорію за `id` (кеш TTL 10 хв)
  2. Перевірити активність і відсутність soft-delete
  3. Повернути `CategoryDto` або 404
- **Side effects (синхронно):** read-through кеш
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Category filter (planned)
  - API-модуль: `frontend/src/features/storefront/api/catalog.api.ts`
  - Статус: `partial`
- **Повертає:** **200** `CategoryDto`.
- **Кеш:** `catalog:categories:id:{id}`, TTL **10 хв**.
- **Помилки:** **404** `Category not found`.

## `GET /catalog/companies/{companyId}/products/{productId}/availability`

- **Summary (1 рядок):** Агрегована наявність товару по складах компанії.
- **Призначення:** агрегована наявність товару по **усіх складах** компанії (сума `Available` по рядках `WarehouseStock`).
- **Хто може викликати:**
  - JWT: не потрібна
  - Глобальні ролі: —
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Агрегувати `Available` по всіх складах компанії
  2. Обчислити `availabilityStatus` (out_of_stock / low_stock / in_stock)
  3. Повернути `ProductAvailabilityDto`
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Product detail (planned)
  - API-модуль: `frontend/src/features/storefront/api/catalog.api.ts`
  - Статус: `partial`
- **Приймає:** path `companyId` (guid), `productId` (long).
- **Повертає:** `ProductAvailabilityDto`: `productId`, `availableQty`, `availabilityStatus`:
  - `out_of_stock` якщо `availableQty <= 0`
  - `low_stock` якщо `1..5`
  - `in_stock` якщо `> 5`
- **Примітки:** логіка статусу збігається з `GetCatalogProductsQueryHandler` для вітрини.

## `GET /catalog/products`

- **Summary (1 рядок):** Публічний список активних товарів з наявністю.
- **Призначення:** публічний список **активних** товарів з агрегованою наявністю зі складів.
- **Хто може викликати:**
  - JWT: не потрібна
  - Глобальні ролі: —
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Завантажити активні товари (кеш списку TTL 5 хв)
  2. Агрегувати наявність зі складів
  3. Повернути масив `ProductListItemDto`
- **Side effects (синхронно):** кеш списку **`catalog:products:list`**, TTL **5 хвилин**; при зміні товарів/стоку кеш скидається (див. handlers продуктів та інвентарю)
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: HomeScreen, ProductsScreen
  - API-модуль: `frontend/src/features/storefront/api/catalog.api.ts`
  - Статус: `implemented`
- **Повертає:** масив `ProductListItemDto` (у т.ч. `availableQty`, `availabilityStatus`).

## `GET /catalog/products/search`

- **Summary (1 рядок):** Пошук товарів через Elasticsearch з fallback на БД.
- **Призначення:** пошук товарів через Elasticsearch з фільтрами та пагінацією.
- **Хто може викликати:**
  - JWT: не потрібна
  - Глобальні ролі: —
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Сформувати ES-запит з фільтрами та сортуванням
  2. При деградації ES — fallback на `ListActiveAsync` + фільтрація в застосунку
  3. Повернути `ProductSearchResultDto` з cursor-пагінацією
- **Side effects (синхронно):** читає з Elasticsearch; observability: `catalog_search_fallback_total`
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: ProductsScreen search (planned)
  - API-модуль: `frontend/src/features/storefront/api/catalog.api.ts`
  - Статус: `partial`
- **Приймає (query):**
  - `name` (string, опційно) — пошук **по назві товару** (пріоритетний параметр)
  - `query` (string, опційно) — текст запиту
  - `categoryIds` (repeatable long, опційно)
  - `companyId` (guid, опційно)
  - `minPrice` / `maxPrice` (decimal, опційно)
  - `availabilityStatus` (`out_of_stock` / `low_stock` / `in_stock`, опційно)
  - `sort` (`relevance` / `newest` / `price_asc` / `price_desc`)
  - `page` (int, default 1), `pageSize` (int, default 20)
  - `searchAfter` (base64 cursor, опційно) — cursor для наступної сторінки
- **Повертає:** `ProductSearchResultDto` (`items`, `total`, `page`, `pageSize`, `nextSearchAfter`).
- **Fallback policy (production):**
  - запит не падає одразу на 5xx через ES-деградацію;
  - результат формується через `ListActiveAsync` + фільтрацію/сортування в застосунку;
  - в observability фіксуються `catalog_operations_total`, `catalog_errors_total`, `catalog_latency_ms`, `catalog_search_fallback_total`.
- **Примітки:** `name` комбінується з `categoryIds` як AND-фільтр (пошук по назві всередині вибраних категорій).

## `GET /catalog/products/{slug}`

- **Summary (1 рядок):** Детальна картка товару за slug.
- **Призначення:** картка товару за `slug` (деталі, зображення) + наявність зі складів.
- **Хто може викликати:**
  - JWT: не потрібна
  - Глобальні ролі: —
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти активний товар за `slug` (кеш detail)
  2. Завантажити деталі, зображення та наявність
  3. Повернути `ProductDto`
- **Side effects (синхронно):** кеш деталі за ключем `catalog:products:detail:{slug}` (префікс `ProductDetailPrefix`)
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Product detail (planned route)
  - API-модуль: `frontend/src/features/storefront/api/catalog.api.ts`
  - Статус: `partial`
- **Приймає:** path `slug` (string).
- **Повертає:** `ProductDto` (композиція продукту; структура в коді — `ProductMapper`).
- **Примітки:** кеш detail також інвалідується після inventory-мутацій (`receive/ship/reserve/release/adjust/transfer`), щоб уникнути stale `availabilityStatus`.

## `GET /catalog/products/{id}/similar`

- **Summary (1 рядок):** Схожі товари за `productId` через Elasticsearch More Like This.
- **Призначення:** рекомендації «схожі товари» для картки товару.
- **Хто може викликати:**
  - JWT: не потрібна
- **Бізнес-логіка:**
  1. Знайти активний товар за `id`
  2. Виконати ES-запит `more_like_this` по `name`, `description`, `tags`, `brands` з фільтрами `categoryId`, price band, exclude self
  3. При деградації ES — fallback на БД (same category + tag/brand scoring)
- **Side effects (синхронно):** кеш `catalog:products:similar:{productId}`, TTL 15 хв; observability: `catalog_similar_products_fallback_total`
- **Приймає:** path `id` (long), query `limit` (default 12, max 24)
- **Повертає:** `SimilarProductsResultDto` (`sourceProductId`, `items[]`)

## `GET /catalog/products/{slug}/similar`

- **Summary (1 рядок):** Схожі товари за `slug` (той самий алгоритм, що й для `id`).
- **Призначення:** рекомендації для storefront, де маршрут побудований на slug.
- **Хто може викликати:**
  - JWT: не потрібна
- **Бізнес-логіка:** resolve slug → active product → similar products pipeline (див. endpoint за `id`)
- **Приймає:** path `slug` (string), query `limit` (default 12, max 24)
- **Повертає:** `SimilarProductsResultDto`

## `GET /catalog/recommendations/me`

- **Summary (1 рядок):** Персональні рекомендації для поточного авторизованого користувача.
- **Призначення:** повернути персоналізовану стрічку товарів на основі активної ML.NET моделі; при недоступній моделі — fallback.
- **Хто може викликати:**
  - JWT: обов'язковий
  - Глобальні ролі: будь-який авторизований користувач
  - Компанійні ролі: будь-які
- **Бізнес-логіка:**
  1. Взяти `userId` з JWT
  2. Завантажити активну recommendation model (`model.zip`)
  3. Обчислити top-K рекомендацій або застосувати fallback, якщо модель недоступна
  4. Повернути `PersonalizedRecommendationsResultDto`
- **Side effects (синхронно):** observability для inference latency/fallback rate.
- **Async / «магія»:** модель тренується і промотується фоновими Hangfire jobs.
- **Приймає:** query `limit` (default 12).
- **Повертає:** `PersonalizedRecommendationsResultDto` (`userId`, `modelVersion`, `usedFallback`, `items[]`).

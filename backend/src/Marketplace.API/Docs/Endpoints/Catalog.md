# CatalogController — `/catalog`

Усі маршрути: **`[AllowAnonymous]`** (публічний вітринний API).

## `GET /catalog/companies`

- **Призначення:** публічний список **схвалених** компаній (без soft-deleted).
- **Повертає:** масив `CompanyDto`.
- **Side effects:** можливе читання з кешу (`catalog:companies:approved` — див. `CatalogCacheKeys` у застосунку).
- **Примітки:** не показує неапрувнуті компанії.

## `GET /catalog/companies/{idOrSlug}`

- **Призначення:** публічна картка компанії за **guid** або **slug** (якщо сегмент парситься як `guid`, використовується пошук за id).
- **Повертає:** **200** `CompanyDto` лише для **схваленої** та не soft-deleted компанії.
- **Кеш:** `catalog:companies:id:{guid}` або `catalog:companies:slug:{slug}`, TTL **10 хв**.
- **Помилки:** **404** `Company not found`.

## `GET /catalog/categories`

- **Призначення:** **активні** категорії для вітрини.
- **Повертає:** масив `CategoryDto`.
- **Side effects:** можливе читання з кешу (`catalog:categories:active`).

## `GET /catalog/categories/{id}`

- **Призначення:** публічна деталь категорії за числовим id — лише **активна** та не soft-deleted.
- **Повертає:** **200** `CategoryDto`.
- **Кеш:** `catalog:categories:id:{id}`, TTL **10 хв**.
- **Помилки:** **404** `Category not found`.

## `GET /catalog/companies/{companyId}/products/{productId}/availability`

- **Призначення:** агрегована наявність товару по **усіх складах** компанії (сума `Available` по рядках `WarehouseStock`).
- **Приймає:** path `companyId` (guid), `productId` (long).
- **Повертає:** `ProductAvailabilityDto`: `productId`, `availableQty`, `availabilityStatus`:
  - `out_of_stock` якщо `availableQty <= 0`
  - `low_stock` якщо `1..5`
  - `in_stock` якщо `> 5`
- **Side effects:** немає (read-only з БД).
- **Примітки:** логіка статусу збігається з `GetCatalogProductsQueryHandler` для вітрини.

## `GET /catalog/products`

- **Призначення:** публічний список **активних** товарів з агрегованою наявністю зі складів.
- **Повертає:** масив `ProductListItemDto` (у т.ч. `availableQty`, `availabilityStatus`).
- **Side effects:** кеш списку **`catalog:products:list`**, TTL **5 хвилин**; при зміні товарів/стоку кеш скидається (див. handlers продуктів та інвентарю).

## `GET /catalog/products/search`

- **Призначення:** пошук товарів через Elasticsearch з фільтрами та пагінацією.
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
- **Side effects:** читає з Elasticsearch; якщо ES недоступний, fallback на БД-логіку каталогу.
- **Примітки:** `name` комбінується з `categoryIds` як AND-фільтр (пошук по назві всередині вибраних категорій).

## `GET /catalog/products/{slug}`

- **Призначення:** картка товару за `slug` (деталі, зображення) + наявність зі складів.
- **Приймає:** path `slug` (string).
- **Повертає:** `ProductDto` (композиція продукту; структура в коді — `ProductMapper`).
- **Side effects:** кеш деталі за ключем `catalog:products:detail:{slug}` (префікс `ProductDetailPrefix`).
- **Примітки:** кеш detail також інвалідується після inventory-мутацій (`receive/ship/reserve/release/adjust/transfer`), щоб уникнути stale `availabilityStatus`.

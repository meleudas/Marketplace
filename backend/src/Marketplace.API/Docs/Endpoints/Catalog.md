# CatalogController — `/catalog`

Усі маршрути: **`[AllowAnonymous]`** (публічний вітринний API).

## `GET /catalog/companies`

- **Призначення:** публічний список **схвалених** компаній.
- **Повертає:** масив `CompanyDto`.
- **Side effects:** можливе читання з кешу (`catalog:companies:approved` — див. `CatalogCacheKeys` у застосунку).
- **Примітки:** не показує неапрувнуті компанії.

## `GET /catalog/categories`

- **Призначення:** **активні** категорії для вітрини.
- **Повертає:** масив `CategoryDto`.
- **Side effects:** можливе читання з кешу (`catalog:categories:active`).

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

## `GET /catalog/products/{slug}`

- **Призначення:** картка товару за `slug` (деталі, зображення) + наявність зі складів.
- **Приймає:** path `slug` (string).
- **Повертає:** `ProductDto` (композиція продукту; структура в коді — `ProductMapper`).
- **Side effects:** кеш деталі за ключем `catalog:products:detail:{slug}` (префікс `ProductDetailPrefix`).

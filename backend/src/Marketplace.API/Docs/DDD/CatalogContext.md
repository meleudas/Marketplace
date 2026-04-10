# Bounded context: Catalog (публічна вітрина)

## Призначення

**Читати** узгоджений публічний вигляд маркетплейсу: схвалені компанії, активні категорії, список і картка товару, публічна наявність.

## Ключові правила

- Компанії в каталозі — лише **`isApproved`**.
- Категорії — лише **`isActive`** (і не deleted — залежить від репозиторію).
- Товари на вітрині — **активні** продукти з домену каталогу; наявність **не** з одного поля продукту, а з **агрегації складських залишків** (`WarehouseStock.Available`).

## Кеш

Ключі (`CatalogCacheKeys`):

| Ключ | Призначення |
|------|-------------|
| `catalog:companies:approved` | Схвалені компанії (якщо використовується в query) |
| `catalog:categories:active` | Активні категорії |
| `catalog:products:list` | Список товарів вітрини, TTL **5 хв** |
| `catalog:products:detail:{slug}` | Деталь картки за slug |

**Інвалідація:** команди **продуктів** (create/update/delete) та **інвентарні** операції, що змінюють доступну кількість, знімають `catalog:products:list`; продуктові команди також чистять ключ деталі за slug.

## Зв’язки

- **Products context** — джерело списку активних `Product`.
- **Inventory context** — джерело сумарної наявності по `CompanyId` + `ProductId`.

## HTTP API

[Endpoints/Catalog.md](../Endpoints/Catalog.md)

## Код

- `Marketplace.API/Controllers/CatalogController.cs`
- `Marketplace.Application/Products/Queries/GetCatalogProducts/*`
- `Marketplace.Application/Products/Queries/GetCatalogProductBySlug/*`
- `Marketplace.Application/Inventory/Queries/GetProductAvailability/*`
- `Marketplace.Application/Catalog/Cache/CatalogCacheKeys.cs`

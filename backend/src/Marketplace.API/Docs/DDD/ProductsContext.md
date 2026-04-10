# Bounded context: Products (товари компанії та відображення)

## Призначення

Управління **каталожною** сутністю `Product` у межах **однієї компанії**: назва, slug, ціна, категорія, мінімальний сток, деталі, зображення, статус (активний/soft-delete).

## Межі (boundary)

- `companyId` у маршруті визначає власника товару.
- Доступ до операцій — через **членство** і **компанійну роль** (`ProductAccessService`), окрім **Admin**.

## Зв’язки

- **Company** — власник `Product.CompanyId`.
- **Category** — `CategoryId` має існувати (валідація в команді).
- **Inventory** — наявність на вітрині та у внутрішніх списках рахується з **WarehouseStock**, а не лише з `MinStock` на продукті.
- **Catalog (публічний)** — читає активні продукти й підтягує сток.

## Side effects на вітрину

Після змін продукту оновлюється кеш:

- `catalog:products:list`
- `catalog:products:detail:{slug}` (старий і новий slug при перейменуванні)

## HTTP API

[Endpoints/Products.md](../Endpoints/Products.md)

## Код

- `Marketplace.API/Controllers/ProductsController.cs`
- `Marketplace.Application/Products/**`
- `Marketplace.Domain/Catalog/Entities/Product.cs`

# Domain Report: Catalog & Categories

- Статус реалізації: `Implemented`
- Готовність: **86/100**
- Release status: `Conditional`
- Confidence: `High`
- Дата аудиту: 2026-06-29

## Evidence

- CI: `catalog-categories-gate`
- Container: ElasticsearchSimilarProductsTests, AdminCatalogReindexContainersTests

## Межі домену

- `backend/src/Marketplace.Application/Products/Queries`
- `backend/src/Marketplace.Application/Categories`
- `backend/src/Marketplace.API/Controllers/CatalogController.cs`
- `backend/src/Marketplace.Infrastructure/Search`
- `backend/src/Marketplace.API/Docs/Endpoints/Catalog.md`

## Що вже готово

- Публічний каталог, пошук, фільтрація, агрегація видимості товарів.
- Інтеграція з cache та опціональним Elasticsearch.
- Базові API та query-тести на читання каталогу.
- Формалізований fallback ES -> DB із логуванням деградації.
- Закриті edge-cases категорій (parent validation, self-parent, delete guards for children/products).
- Розширене API + contract + integration покриття з `Suite=CatalogCategories`.
- Введено окремий CI gate `catalog-categories-gate`.
- Додано метрики/алерти для catalog latency/errors/fallback.

## Blockers

- Немає (закрито).

## Near-term

- Моніторити стабільність `catalog-categories-gate` протягом 1-2 спринтів.
- Підкрутити поріг coverage gate після накопичення regression-статистики.

## Optional

- Винести categories у окремий детальний DDD-doc контекст.
- Додати AB-ready hooks для ranking.

## Мінімальний checklist

- [x] Catalog endpoints мають API + contract тести.
- [x] Працює fallback без Elasticsearch.
- [x] Cache invalidation визначено й перевірено.
- [x] Доступ для anonymous/read-only контрольований.

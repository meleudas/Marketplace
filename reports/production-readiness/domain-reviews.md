# Domain Report: Reviews

- Статус: `Implemented`
- Оцінка готовності: **100/100**

## Межі домену

- `backend/src/Marketplace.Domain/Reviews`
- `backend/src/Marketplace.Application/Reviews`
- `backend/src/Marketplace.API/Controllers/ProductReviewsController.cs`
- `backend/src/Marketplace.API/Controllers/CompanyReviewsController.cs`
- `backend/src/Marketplace.API/Controllers/AdminReviewsController.cs`

## Що вже готово

- Закритий review lifecycle для `product/company`: `create -> update/delete own -> reply -> moderate` з route-scope перевірками і edge-handling (`not_found/forbidden/unauthorized`).
- Закритий data-integrity ризик soft-delete re-create: active-only unique indexes для `product_reviews` і `company_reviews` + міграція.
- Розширено test contour до production-ready рівня: `domain + application + API + SQLite integration + contract + security + performance`.
- Введено профільний `Suite=Reviews` і додано окремий `reviews-gate` у CI з coverage threshold `12%`.
- Додано review observability trio: `review_operations_total`, `review_errors_total`, `review_latency_ms`; інструментовано review controllers.
- Оновлені endpoint/docs та DDD контекст (`ReviewsContext`) під фактичну поведінку домену.

## Blockers

Blockers закриті.

## Near-term

- Моніторити `reviews-gate` coverage trend і тримати поріг не нижче `12%`.
- Тримати alerts на `review_errors_total`/`review_latency_ms` після релізів та schema changes.

## Optional

- Додати anti-abuse евристику й автомодерацію.
- Додати аналітичний звіт по якості review.
- Додати нотифікації по review moderation/reply як окремий крос-доменний етап.

## Мінімальний checklist

- [x] Модерація має негативні authz тести.
- [x] Верифікація покупки тестується end-to-end.
- [x] Public/private view правилa задокументовані контрактно.

# Bounded context: Reviews

## Призначення

Домен `Reviews` відповідає за:
- створення відгуків на товар/компанію;
- публічні списки відгуків;
- офіційні відповіді компанії на відгуки;
- модерацію (`Admin`/`Moderator`) і контроль публічної видимості.

## Інваріанти

- Один активний відгук на `(productId, userId)` або `(companyId, userId)`.
- Відгук створюється лише для `verified purchase`.
- Публічний список повертає лише `Approved` відгуки.
- `update/delete` дозволені лише власнику відгуку і лише для коректного route scope (`productId/companyId`).
- `reply` дозволено `Owner|Manager|Seller` відповідної компанії або `Admin`.

## Side effects

- Після create/update/delete/moderate перераховуються rating/reviewCount цільового продукту/компанії.
- Інвалідуються cache-ключі каталогу/відгуків:
  - `catalog:products:list`
  - `catalog:products:detail:{slug}`
  - `catalog:products:reviews:{productId}:{page}:{size}`
  - `catalog:companies:reviews:{companyId}:{page}:{size}`

## HTTP API

- [Endpoints/Reviews.md](../Endpoints/Reviews.md)

## Код

- `Marketplace.API/Controllers/ProductReviewsController.cs`
- `Marketplace.API/Controllers/CompanyReviewsController.cs`
- `Marketplace.API/Controllers/ReviewRepliesController.cs`
- `Marketplace.API/Controllers/AdminReviewsController.cs`
- `Marketplace.Application/Reviews/**`
- `Marketplace.Domain/Reviews/**`
- `Marketplace.Domain/Companies/Entities/CompanyReview.cs`

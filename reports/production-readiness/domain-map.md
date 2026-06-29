# Карта backend-доменів

Оновлено: **2026-06-29** (після container test expansion).

## Реалізовані домени

| Домен | Domain | Application | API | Infrastructure | Звіт |
|---|---|---|---|---|---|
| Identity & Access | `Domain/Auth`, `Domain/Users` | `Application/Auth`, `Application/Users` | `AuthController`, `AccountController`, `ExternalAuthController`, `UsersController` | `Infrastructure/Identity` | [domain-identity-access.md](./domain-identity-access.md) |
| Companies & Workspace | `Domain/Companies` | `Application/Companies` | `CompanyMembersController`, `AdminCatalogController` | `Repositories/Company*` | [domain-companies-workspace.md](./domain-companies-workspace.md) |
| Catalog & Categories | `Domain/Catalog`, `Domain/Categories` | `Application/Products/Queries`, `Application/Categories` | `CatalogController` | `External/Search`, `CategoryRepository` | [domain-catalog-categories.md](./domain-catalog-categories.md) |
| Products & Moderation | `Domain/Catalog` | `Application/Products` | `ProductsController`, `AdminProductsController` | `Repositories/Product*` | [domain-products-moderation.md](./domain-products-moderation.md) |
| Inventory | `Domain/Inventory` | `Application/Inventory` | `InventoryController` | `Repositories/*Stock*` | [domain-inventory.md](./domain-inventory.md) |
| Cart & Checkout | `Domain/Cart` | `Application/Carts` | `CartController` | `Repositories/Cart*` | [domain-cart-checkout.md](./domain-cart-checkout.md) |
| Favorites | `Domain/Favorites` | `Application/Favorites` | `FavoritesController` | `FavoriteRepository` | [domain-favorites.md](./domain-favorites.md) |
| Orders | `Domain/Orders` | `Application/Orders` | `OrdersController` | `Repositories/Order*` | [domain-orders.md](./domain-orders.md) |
| Payments | `Domain/Payments` | `Application/Payments` | `PaymentsIntegrationsController`, `AdminPaymentsController` | `Payments`, `PaymentJobs` | [domain-payments.md](./domain-payments.md) |
| Reviews | `Domain/Reviews` | `Application/Reviews` | `ProductReviewsController`, `CompanyReviewsController`, `AdminReviewsController` | `Repositories/*Review*` | [domain-reviews.md](./domain-reviews.md) |
| Notifications | `Domain/Notifications` | `Application/Notifications` | `PushNotificationsController`, `MeNotificationsController` | `Notifications`, `AppNotificationJobs` | [domain-notifications.md](./domain-notifications.md) |
| Platform | крос-доменний | `Application/Common` | `AdminOutboxController` | `Jobs`, `OutboxRepository`, `HttpIdempotencyStore` | [domain-platform.md](./domain-platform.md) |
| Shipping | `Domain/Shipping` | `Application/Shipping` | `ShippingController`, `UserAddressesController` | `Repositories/Shipment*`, Nova Poshta client | [domain-shipping.md](./domain-shipping.md) |
| Coupons | `Domain/Coupons` | `Application/Coupons` | `CartController` (coupons), `AdminCouponsController` | `CouponRepository`, rules | [domain-coupons.md](./domain-coupons.md) |
| Returns (RMA) | `Domain/Returns` | `Application/Returns` | `ReturnsController` | `ReturnRequestRepository` | [domain-returns.md](./domain-returns.md) |
| Reports (moderation) | `Domain/Reports` | `Application/Reports` | `ReportsController`, admin moderation | `ReportRepository` | [domain-reports-moderation.md](./domain-reports-moderation.md) |
| Chats | `Domain/Chats` | `Application/Chats` | `ChatsController` | `ChatRepository` | [domain-chats.md](./domain-chats.md) |
| Support | `Domain/Support` | `Application/Support` | Port contract | `SupportTicket*` repositories | [domain-support.md](./domain-support.md) |
| Behavior/Analytics & Recommendations | `Domain/Behavior` | `Application/Behavior`, `Products/Ports` | `AnalyticsController`, recommendations endpoints | `ClickHouse*`, `RecommendationModel*` | [domain-analytics-recommendations.md](./domain-analytics-recommendations.md) |

## Залишкові прогалини

Див. [domain-gaps-and-proposals.md](./domain-gaps-and-proposals.md) — лише нефункціональні та P2 покращення.

## Наскрізні звіти

- [infrastructure-services-readiness.md](./infrastructure-services-readiness.md)
- [external-integrations-readiness.md](./external-integrations-readiness.md)
- [security-readiness.md](./security-readiness.md)
- [testing-quality-readiness.md](./testing-quality-readiness.md)
- [devops-cicd-readiness.md](./devops-cicd-readiness.md)

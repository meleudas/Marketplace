# Карта backend-доменів

Цей файл фіксує цільову карту доменів backend та їх зіставлення з шарами `Domain`, `Application`, `API`, `Infrastructure`.

## Реалізовані домени

| Домен | Domain | Application | API | Infrastructure |
|---|---|---|---|---|
| Identity & Access | `backend/src/Marketplace.Domain/Auth`, `backend/src/Marketplace.Domain/Users` | `backend/src/Marketplace.Application/Auth`, `backend/src/Marketplace.Application/Users` | `backend/src/Marketplace.API/Controllers/AuthController.cs`, `AccountController.cs`, `ExternalAuthController.cs`, `UsersController.cs` | `backend/src/Marketplace.Infrastructure/Identity` |
| Companies & Workspace | `backend/src/Marketplace.Domain/Companies` | `backend/src/Marketplace.Application/Companies` | `backend/src/Marketplace.API/Controllers/AdminCatalogController.cs`, `CompanyMembersController.cs` | `backend/src/Marketplace.Infrastructure/Persistence/Repositories/Company*` |
| Catalog & Categories | `backend/src/Marketplace.Domain/Catalog`, `backend/src/Marketplace.Domain/Categories` | `backend/src/Marketplace.Application/Products/Queries`, `backend/src/Marketplace.Application/Categories` | `backend/src/Marketplace.API/Controllers/CatalogController.cs` | `backend/src/Marketplace.Infrastructure/Search`, `.../Repositories/CategoryRepository.cs` |
| Products & Moderation | `backend/src/Marketplace.Domain/Catalog` | `backend/src/Marketplace.Application/Products` | `backend/src/Marketplace.API/Controllers/ProductsController.cs`, `AdminProductsController.cs` | `backend/src/Marketplace.Infrastructure/Persistence/Repositories/Product*` |
| Inventory | `backend/src/Marketplace.Domain/Inventory` | `backend/src/Marketplace.Application/Inventory` | `backend/src/Marketplace.API/Controllers/InventoryController.cs` | `backend/src/Marketplace.Infrastructure/Persistence/Repositories/*Stock*` |
| Cart & Checkout | `backend/src/Marketplace.Domain/Cart` | `backend/src/Marketplace.Application/Carts` | `backend/src/Marketplace.API/Controllers/CartController.cs` | `backend/src/Marketplace.Infrastructure/Persistence/Repositories/Cart*` |
| Favorites | `backend/src/Marketplace.Domain/Favorites` | `backend/src/Marketplace.Application/Favorites` | `backend/src/Marketplace.API/Controllers/FavoritesController.cs` | `backend/src/Marketplace.Infrastructure/Persistence/Repositories/FavoriteRepository.cs` |
| Orders | `backend/src/Marketplace.Domain/Orders` | `backend/src/Marketplace.Application/Orders` | `backend/src/Marketplace.API/Controllers/OrdersController.cs` | `backend/src/Marketplace.Infrastructure/Persistence/Repositories/Order*` |
| Payments | `backend/src/Marketplace.Domain/Payments` | `backend/src/Marketplace.Application/Payments` | `backend/src/Marketplace.API/Controllers/PaymentsIntegrationsController.cs`, `AdminPaymentsController.cs` | `backend/src/Marketplace.Infrastructure/Payments`, `.../Jobs/PaymentJobs.cs` |
| Reviews | `backend/src/Marketplace.Domain/Reviews` | `backend/src/Marketplace.Application/Reviews` | `backend/src/Marketplace.API/Controllers/ProductReviewsController.cs`, `CompanyReviewsController.cs`, `AdminReviewsController.cs` | `backend/src/Marketplace.Infrastructure/Persistence/Repositories/*Review*` |
| Notifications | `backend/src/Marketplace.Domain/Notifications` | `backend/src/Marketplace.Application/Notifications` | `backend/src/Marketplace.API/Controllers/PushNotificationsController.cs`, `MeNotificationsController.cs` | `backend/src/Marketplace.Infrastructure/Notifications`, `.../Jobs/AppNotificationJobs.cs` |
| Platform (Outbox/Idempotency/Jobs) | крос-доменний | `backend/src/Marketplace.Application/Common` | `backend/src/Marketplace.API/Controllers/AdminOutboxController.cs` | `backend/src/Marketplace.Infrastructure/Jobs`, `.../Persistence/Repositories/OutboxRepository.cs`, `HttpIdempotencyStore.cs` |

## Planned/Gap домени

| Домен | Поточний стан |
|---|---|
| Shipping | Є доменна модель, відсутній повний application/api/infra стек |
| Coupons | Є доменні сутності, відсутні use-cases і API |
| Chats | Є доменна модель, відсутні handlers/контролери |
| Support | Є доменна модель, відсутній прикладний шар |
| Reports | Є доменна модель, відсутній API/процеси модерації |
| Behavior/Analytics | Є доменні сутності, відсутній operational data pipeline |

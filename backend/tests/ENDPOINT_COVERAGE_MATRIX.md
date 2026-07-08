# Endpoint coverage matrix (P0 / P1)

Legend: **U** = Unit.Api, **L** = Integration.Light, **C** = Integration.Containers, **E** = E2E

| Route | U | L | C | E | Notes |
|-------|---|---|---|---|-------|
| `GET /health` | | | | E | HealthE2ETests |
| `GET /health/liqpay/config` | | | | E | HealthE2ETests |
| `GET /me/cart` | U | L | C | E | Cart checkout + E2E auth |
| `POST /me/cart/items` | U | | | | Api CartController |
| `PATCH /me/cart/items/{id}` | U | | | | |
| `DELETE /me/cart/items/{id}` | U | | | | |
| `POST /me/cart/coupons/validate` | U | L | C | | CouponEligibilityPostgresTests |
| `POST /me/cart/coupons/apply` | U | L | C | | CouponEligibilityPostgresTests |
| `DELETE /me/cart/coupons/{code}` | U | L | C | | CouponEligibilityPostgresTests |
| `POST /me/cart/checkout` | U | L | C | | CheckoutPostgresTests |
| `POST /admin/coupons` | U | L | | | Admin coupon create |
| `PATCH /admin/coupons/{id}` | U | | | | Admin coupon update |
| `POST /admin/coupons/{id}/deactivate` | U | | | | Emergency deactivate |
| `GET /admin/coupons/{id}/usage` | U | | | | Coupon usage report |
| `GET /me/addresses` | U | L | | | Shipping address queries |
| `POST /me/addresses` | U | L | | | Shipping address create |
| `PATCH /me/addresses/{id}` | U | | | | Shipping address update |
| `DELETE /me/addresses/{id}` | U | | | | Shipping address delete |
| `POST /me/addresses/{id}/default` | U | | | | Shipping default policy |
| `GET /shipping/methods` | U | L | | | Active shipping methods |
| `POST /shipping/quote` | U | L | | E | Shipping quote + NovaPoshtaSandboxE2ETests (Staging) |
| `GET /me/shipments` | U | | | | Shipment list |
| `POST /integrations/shipping/novaposhta/webhook` | U | | C | | Webhook dedup |
| `POST /integrations/liqpay/webhook` | U | L | C | E | Handler + HTTP + LiqPaySandboxE2ETests (Staging) |
| `POST /integrations/telegram/webhook` | | | | E | Secret header |
| `POST /auth/register` | U | | C | E | AuthFlowPostgresTests |
| `POST /auth/login` | U | | C | E | AuthFlowPostgresTests |
| `POST /auth/refresh` | U | | C | | AuthFlowPostgresTests |
| `POST /auth/external/google/callback` | | | C | | ExternalAuthCallbackPostgresTests |
| `GET /me/orders` | U | L | | E | |
| `GET /me/orders/{id}` | U | L | | | |
| `POST /orders/{id}/status` | U | L | C | | Outbox + notification schedule |
| `GET /me/in-app-notifications` | | L | C | E | After status / direct insert |
| `GET /catalog/products/search` | U | L | C | E | ApplicationProductSearchQueryTests + CatalogSearchFilterCombinationContainersTests + CatalogSearchFilterCombinationE2ETests |
| `GET /catalog/products/{slug}` | U | | | | |
| `GET /me/push-subscriptions/vapid-public-key` | | | C | E | PushSubscriptionPostgresTests |
| `POST /me/push-subscriptions` | | | C | | PushSubscriptionPostgresTests |
| `GET /me/favorites` | U | | C | | FavoritesPostgresTests |
| `POST /me/favorites/{productId}` | U | | C | | FavoritesPostgresTests |
| `DELETE /me/favorites/{productId}` | U | | C | | FavoritesPostgresTests |
| `POST /catalog/products/{id}/reviews` | U | L | C | | ReviewsModerationPostgresTests |
| `POST /admin/reports/{id}/resolve` | U | | C | | ReportsWorkflowPostgresTests |
| `POST /reports` | U | | C | | ReportsWorkflowPostgresTests |
| `GET /me/returns` | U | | C | E | ReturnRequestWorkflowPostgresTests |
| `POST /admin/products/{id}/approve` | U | L | C | | AdminCatalogReindexContainersTests |
| `GET /catalog/products/similar/{id}` | U | | C | | ElasticsearchSimilarProductsTests |
| `GET /catalog/products/recommendations` | | | C | | RecommendationPipelineContainersTests |
| `GET /companies/{id}/members` | U | | C | | CompanyWorkspacePostgresTests |
| Outbox dispatch job | U | | C | | OutboxDispatcherJobs + Postgres |
| Integration retry processor | | | C | | IntegrationRetryProcessorPostgresTests |
| Redis rate limiting | | | C | | RedisRateLimitingContainersTests |
| ClickHouse analytics warehouse | | | C | | ClickHouseWarehousePipelineContainersTests |
| ML recommendation training | | | C | | RecommendationPipelineContainersTests |
| Inventory expire job | | | C | | JobSmokePostgresTests |
| Payment sync job | | | C | | JobSmokePostgresTests |
| Elasticsearch reachability | | | C | | Ping container |

## P4 finance / fulfillment (seed-backed)

| Route | U | L | C | E | Notes |
|-------|---|---|---|---|-------|
| `GET /companies/{id}/earnings/summary` | U | | C | E | SeedFinanceE2ETests + FinanceSettlementPostgresTests |
| `GET /companies/{id}/settlements` | U | | | E | SeedFinanceE2ETests |
| `PATCH /companies/{id}/payout-profile` | U | | | | ApiCompanyFinanceControllerTests |
| `GET /admin/settlements` | U | | | E | SeedFinanceE2ETests |
| `POST /admin/settlements/{id}/approve-payout` | U | | C | | FinanceSettlementPostgresTests |
| `GET /companies/{id}/orders/{orderId}` (fulfillment) | | | C | E | SeedP4FulfillmentE2ETests |
| `GET /companies/{id}/orders/{orderId}/shipments` | U | | C | E | Seed + ShipmentFulfillmentPostgresTests |
| `POST /companies/{id}/orders/{orderId}/shipments` | U | | C | | ShipmentFulfillmentPostgresTests |
| `GET /me/orders/{orderId}/shipments` | U | | | E | SeedP4FulfillmentE2ETests |
| `GET /me/returns` | U | | | E | SeedCouponReturnChatE2ETests |
| `GET /me/chats` | U | | | E | SeedCouponReturnChatE2ETests |
| `POST /me/chats/{id}/messages` | U | | | E | SeedCouponReturnChatE2ETests |
| `POST /me/cart/coupons/validate` | U | L | | E | Seed SEED10 |
| `GET /catalog/products/{slug}` | U | | | E | SeedCatalogE2ETests |
| `GET /me/orders/{id}` | U | L | | E | SeedOrdersE2ETests |
| All HTTP routes (catalog smoke) | | | | E | ApiEndpointCatalogSmokeTests (`Suite=ApiCatalogSmoke`) |

Update this table when adding or changing routes in `src/Marketplace.API/Docs/Endpoints/`.

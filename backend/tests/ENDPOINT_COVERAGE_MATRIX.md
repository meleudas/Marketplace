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
| `POST /me/cart/coupons/validate` | U | L | | | Coupons validate |
| `POST /me/cart/coupons/apply` | U | L | | | Coupons apply |
| `DELETE /me/cart/coupons/{code}` | U | L | | | Coupons remove |
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
| `POST /shipping/quote` | U | L | | | Shipping quote calculation |
| `GET /me/shipments` | U | | | | Shipment list |
| `POST /integrations/shipping/novaposhta/webhook` | U | | C | | Webhook dedup |
| `POST /integrations/liqpay/webhook` | U | L | C | E | Handler + HTTP |
| `POST /integrations/telegram/webhook` | | | | E | Secret header |
| `POST /auth/register` | U | | | E | |
| `POST /auth/login` | U | | | E | |
| `POST /auth/refresh` | U | | | | Cookie flow |
| `GET /me/orders` | U | L | | E | |
| `GET /me/orders/{id}` | U | L | | | |
| `POST /orders/{id}/status` | U | L | C | | Outbox + notification schedule |
| `GET /me/in-app-notifications` | | L | C | E | After status / direct insert |
| `GET /catalog/products/search` | U | L | | E | |
| `GET /catalog/products/{slug}` | U | | | | |
| `GET /me/push-subscriptions/vapid-public-key` | | | | E | |
| Outbox dispatch job | U | | C | | OutboxDispatcherJobs + Postgres |
| Inventory expire job | | | C | | JobSmokePostgresTests |
| Payment sync job | | | C | | JobSmokePostgresTests |
| Elasticsearch reachability | | | C | | Ping container |

Update this table when adding or changing routes in `src/Marketplace.API/Docs/Endpoints/`.

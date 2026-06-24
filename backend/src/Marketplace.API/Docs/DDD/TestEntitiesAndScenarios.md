# Тестові сутності та сценарії

## Готовий seed у репозиторії

Повний набір узгоджених даних задається скриптом [backend/scripts/seed-test-data.sql](../../../../scripts/seed-test-data.sql) (запуск: `docker compose --profile tools run --rm db-seed` з кореня репо).  
Пароль **усіх** акаунтів: **`Admin123!`**.

Після міграцій і seed (схема з **uuid** для `companies.Id`):

| Сутність | Значення |
|----------|----------|
| **Tech Store** | `CompanyId` = `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa`, slug `tech-store` |
| **Home Comfort** | `CompanyId` = `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb`, slug `home-comfort` (також **approved**) |
| **Категорії** | `1` Electronics, `2` Smartphones (батько 1), `3` Home |
| **Товари (slug)** | `seed-phone-alpha`, `seed-laptop-beta`, `seed-earbuds-gamma`, `seed-kettle-home`, `seed-watch-pending` (PendingReview), `seed-tablet-rejected` (Draft) |
| **Склади** | Tech: warehouse `1` MAIN-KYIV, `2` SEC-LVIV; Home: `3` HOME-LVIV |
| **Сток split** | product 1 на WH1 (`warehouse_stocks` id=1) і WH2 (id=5, Lviv, 30 шт.) |
| **Резерв (демо)** | `SEED-RES-DEMO` на товар 1 (склад 1), 5 од.; order 4: `order-4-product-1-wh-1/2` |
| **Кошик buyer@** | Товари 1–3; **watch** на laptop (product 2, склад 0 — restock demo) |
| **Замовлення** | `ORD-SEED-0001` … `0004` + `order_status_history` |
| **P4 fulfillment** | order 4 — split allocations WH1+WH2; order 2 — shipments per warehouse |
| **Finance** | `order_financials` (orders 2–3), `seller_ledger_entries`, batches 1–3, payout `MANUAL-SEED-001` |
| **Payout IBAN** | Tech Store: `UA213223130000026007233566001` |
| **Coupon** | `SEED10` (10%, Tech Store) |
| **Return** | id=1 на order 3 (`WrongItem`, Requested) |
| **Chat** | `c1000001-0000-4000-8000-000000000001` (buyer ↔ seller, order 2) |
| **In-app (`notifications`)** | 7 рядків (order, restock, admin, moderation, company) — `GET /me/in-app-notifications` |
| **Web Push (`push_subscriptions`)** | buyer / admin / moderator — demo endpoints |

Детальний список логінів — у [backend/DOCKER_COMMANDS.md](../../../../DOCKER_COMMANDS.md) (розділ про seed).

## Швидка матриця «хто що тестує»

| Логін | Глобально | У Tech Store | Типові перевірки |
|-------|-----------|--------------|------------------|
| `admin@marketplace.test` | Admin | — | `GET /admin/*`, bypass company API |
| `seller@marketplace.test` | Seller | Owner | members, products write, inventory write |
| `manager@marketplace.test` | Seller | Manager | як Owner для цих API |
| `seller2@marketplace.test` | Seller | Seller | products write; inventory лише read |
| `support@marketplace.test` | Buyer | Support | read products/inventory; **не** `GET .../members` |
| `logistics@marketplace.test` | Buyer | Logistics | inventory write; products read |
| `buyer@marketplace.test` | Buyer | Owner у **Home Comfort** | кабінет другої компанії, товар `seed-kettle-home` |
| `moderator@marketplace.test` | Moderator | — | глобальна роль (залежить від ваших політик на маршрутах) |
| `user@marketplace.test` | User | — | `UserName` у Identity: **plainuser** |

## Покрокові сценарії (happy path)

### Restock (кошик → прихід на склад)

1. Під `buyer@marketplace.test`: додати в кошик кількість товару **більшу**, ніж поточний сукупний `availableQty` (перевірка як при checkout) — у БД з’являється рядок `cart_stock_watches` для пари `(user_id, product_id)`.
2. Під `seller@marketplace.test` (Owner/Manager з `WriteStock`): `POST .../inventory/receive` для того ж `productId`, щоб сума `Available` по компанії стала **> 0** (була 0).
3. Очікування: для покупця ставляться застосункові нотифікації (`CartProductBackInStock`) через `IAppNotificationScheduler` / Hangfire — Push + In-app за наявності підписок; повтор того ж сценарію протягом **24 год** не дублює сповіщення (поле `LastNotifiedAtUtc`).
4. Checkout або очищення кошика — watch для користувача видаляються (`DeleteAllForUserAsync` / `SyncWatch`).

### A. Вітрина після seed

1. Без логіну: `GET /catalog/companies` — обидві компанії.
2. `GET /catalog/products` — чотири товари з `availableQty` / `availabilityStatus` зі складу.
3. `GET /catalog/products/seed-phone-alpha` — картка з деталлю/зображенням (seed).

### B. Ролі продуктів (Tech Store)

1. `support@` + Bearer: `GET /companies/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/products` → **200**; `POST` → **403**.
2. `seller2@`: `POST .../products` → **200** (якщо body валідний).

### C. Ролі складу

1. `seller2@`: `GET .../inventory/stocks` → **200**; `POST .../inventory/receive` → **403**.
2. `logistics@`: `POST .../inventory/receive` з новим `operationId` → **200**.

### D. Члени компанії

1. `manager@`: `GET .../members` → **200**.
2. `support@`: той самий виклик → **403** (`Forbidden`).
3. `GET .../members/me` для члена → **200**; `buyer@` для Tech Store (не член) → **404** (`Membership not found`).

### E. Інваріант останнього Owner

1. Спроба змінити/видалити єдиного Owner у компанії → помилка з handler.

### F. Ідемпотентність інвентарю

1. Повтор `POST .../receive` з тим самим `operationId` → **400** / текст про вже оброблену операцію.

### G. Резерв

1. У БД уже є активний резерв `SEED-RES-DEMO`; для нового коду — `POST .../reservations` з унікальним `reservationCode`.
2. `DELETE .../reservations/{code}` — зняття резерву.

### H. Multi-warehouse ship (P4)

1. `buyer@`: `GET /companies/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/orders/4` — `fulfillment.pendingByWarehouse` з WH1 і WH2 (по 1 phone).
2. `logistics@` або `seller@`: `POST .../orders/4/shipments?warehouseId=1` — відправка з Kyiv Main.
3. `buyer@` / `seller@`: `GET .../orders/2/shipments` — два shipment (`NP-SEED-0002-A/B`) з різних складів.

### I. Earnings / settlements (P4)

1. `seller@`: `GET /companies/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/earnings/summary` — available ≈ 13372.22 (sale − refund), platformFees ≈ 1124.78.
2. `seller@`: `GET .../settlements` — batch id=1 (Ready, без payout).
3. `seller@`: `PATCH .../payout-profile` — IBAN уже заповнений, можна оновити інший.
4. `admin@`: `GET /admin/settlements?status=Ready` — batch id=1.
5. `admin@`: `POST /admin/settlements/1/approve-payout` — batch без payout (Tech Store IBAN є).

### J. Coupon / return / chat

1. `buyer@`: `POST /me/cart/coupons` з кодом `SEED10` (кошик 1–3, Tech Store).
2. `buyer@`: `GET /me/returns` — 1 return на order 3 (kettle).
3. `seller@` (Home Comfort owner — `buyer@` для HC): `POST .../returns/1/approve`.
4. `buyer@` / `seller@`: `GET /chats` — чат `c1000001-...` по order 2; `POST /chats/{id}/messages`.

## Негативні кейси (чекліст)

- Невалідний JWT на `[Authorize]` → **401**.
- Чужий `companyId` або відсутнє членство → **403** на products/inventory.
- `PATCH /users/{id}/role` не адміном → **403** (ASP.NET roles).
- Невалідна компанійна роль у body members → **400**.

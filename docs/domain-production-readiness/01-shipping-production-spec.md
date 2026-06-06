# 01 - Shipping Production Spec

## 1. Context and business goals

Shipping замикає checkout lifecycle і відповідає за:

- керування адресами користувача;
- вибір методу доставки в checkout;
- розрахунок вартості та ETA;
- інтеграцію з Nova Poshta для доставок, статусів і ТТН.

Ціль: прибрати "manual shipping" з процесу замовлення, щоб `Order -> Delivery` був повністю керований системою.

## 2. Domain target state

Базові сутності вже існують у Domain:

- `Marketplace.Domain/Shipping/Entities/UserAddress.cs`
- `Marketplace.Domain/Shipping/Entities/ShippingMethod.cs`

Потрібно довести модель до production-рівня:

- `Shipment` aggregate: зв'язок із `Order`, carrier tracking id, current state.
- `ShippingRateQuote` value object: carrier, price, ETA, expiresAt.
- `DeliveryStatus` enum: `Created`, `LabelGenerated`, `InTransit`, `Delivered`, `Failed`, `Returned`.
- Інваріанти:
  - один активний shipment на order;
  - перехід статусів монотонний (без downgrade);
  - адреса замовлення immutable після `LabelGenerated`.

## 3. Application target state

### Команди/запити

- `CreateUserAddress`, `UpdateUserAddress`, `DeleteUserAddress`, `SetDefaultAddress`
- `GetMyAddresses`, `GetShippingMethods`
- `CalculateShippingQuote`
- `SelectShippingMethodForCheckout`
- `CreateShipmentFromOrder`
- `SyncShipmentStatusFromCarrier`
- `GenerateShippingLabel`

### Політики

- Ідемпотентність для `GenerateShippingLabel` і `SyncShipmentStatusFromCarrier`.
- Checkout блокується, якщо `shipping quote` протермінований.
- Команди для shipment виконуються в окремих транзакційних межах із outbox-подіями.

## 4. API target state

### Endpoints (мінімум)

- `GET /me/addresses`
- `POST /me/addresses`
- `PATCH /me/addresses/{id}`
- `DELETE /me/addresses/{id}`
- `POST /me/addresses/{id}/default`
- `GET /shipping/methods`
- `POST /shipping/quote`
- `POST /me/cart/checkout/select-shipping`
- `GET /me/shipments`
- `GET /me/shipments/{id}`
- `POST /integrations/shipping/novaposhta/webhook`

### Authz

- Buyer: власні адреси, quote, власні shipment read.
- Admin/Support: read-only доступ до shipment diagnostics.
- Системний webhook endpoint: HMAC/API-key + IP allowlist.

### Error model

- `400` invalid payload/enum/value ranges.
- `403` чужий ресурс.
- `404` address/method/shipment not found.
- `409` status conflict / immutable state violation.
- `422` shipping not available for destination.

## 5. Infrastructure and data model

### Таблиці

- `user_addresses` (доповнення індексами на `user_id`, `is_default`)
- `shipping_methods` (active methods catalog)
- `shipments` (order_id unique, carrier, tracking_number, status, last_synced_at)
- `shipping_quotes` (short-lived quotes, ttl index/cleanup job)
- `shipping_events` (carrier raw events + dedup hash)

### Міграції та індекси

- unique partial index: one default address per user.
- unique index: one active shipment per order.
- dedup index for carrier webhooks/events (`carrier_event_id` or hash).

### Nova Poshta integration

- Adapter порт `INovaPoshtaPort`:
  - quote API
  - create label (TTN)
  - track status
- Resilience:
  - timeout, retry with jitter backoff, circuit breaker.
  - fallback: cached quotes (до `N` хв) і асинхронний resync статусів.
- Security:
  - secret keys в secret store;
  - payload signing validation.

## 6. Security and abuse resistance

- Rate limits:
  - quote endpoint: per user + per IP.
  - webhook endpoint: strict lower limit, deny-by-default.
- Анти-аб'юз:
  - блок повторних label-create при однаковому `order_id`.
  - audit trail на address mutation і shipment admin actions.
- PII:
  - маскування телефону/ПІБ в логах;
  - retention policy для адрес історії.

## 7. Testing strategy and CI gates

### Unit (Suite=Shipping)

- інваріанти shipment transitions;
- quote expiration rules;
- one-default-address rule.

### IntegrationLight

- CRUD адрес + default-switch;
- checkout з валідним і невалідним shipping selection.

### IntegrationContainers

- Nova Poshta adapter contract tests (mock service container);
- webhook dedup + replay protection;
- outbox event dispatch для shipment updates.

### E2E

- buyer address CRUD -> checkout selection -> shipment read;
- webhook status update відображається в `GET /me/shipments/{id}`.

### CI gate

- новий domain gate `shipping-gate`:
  - Unit + IntegrationLight (`Suite=Shipping`)
  - Coverage threshold >= 12%
- `integration-full` додає Containers + E2E.

## 8. Observability and runbook

### Метрики

- `shipping_operations_total`
- `shipping_errors_total`
- `shipping_latency_ms`
- `shipping_quote_failures_total`
- `shipping_webhook_replays_total`

### Трейси

- spans: `ShippingQuote`, `CreateShipmentLabel`, `SyncShipmentStatus`.
- tags: `carrier`, `destination_region`, `result`.

### Алерти

- quote failure rate > 5% / 10m.
- webhook processing lag > 5m.
- shipment status sync errors spike.

### Runbook

- carrier outage mode (fallback quotes, postpone label generation);
- replay-safe resync job;
- manual recovery procedure from dead-letter.

## 9. Release and rollback strategy

- Feature flags:
  - `ShippingEnabled`
  - `NovaPoshtaEnabled`
- Rollout:
  1. deploy schema + read paths;
  2. enable quote API;
  3. enable label generation;
  4. enable webhook-driven status updates.
- Rollback:
  - disable flags, keep data intact;
  - replay outbox after fix.

## 10. Definition of Done (100/100)

- [ ] Є end-to-end flow: address -> quote -> checkout -> shipment -> status sync.
- [ ] Nova Poshta інтеграція має timeout/retry/circuit-breaker + secrets policy.
- [ ] Всі write endpoints мають idempotency або dedup policy.
- [ ] Є Unit/Integration/E2E покриття та `Suite=Shipping` gate.
- [ ] Є метрики, алерти, runbook, rollback playbook.
- [ ] Всі публічні shipping endpoints задокументовані в API docs.

# Domain Report: Shipping

- Статус реалізації: `Implemented`
- Готовність: **85/100**
- Release status: `Conditional`
- Confidence: `High`
- Дата аудиту: 2026-06-29

## Evidence

- CI: `shipping-gate` у [backend-ci.yml](../../.github/workflows/backend-ci.yml)
- Container: ShipmentFulfillmentPostgresTests, NovaPoshtaWebhookPostgresTests
- Matrix: addresses, quotes, webhooks — U/L/C; E2E seed fulfillment

## Межі домену

- `backend/src/Marketplace.Domain/Shipping`
- `backend/src/Marketplace.Application/Shipping`
- `backend/src/Marketplace.API/Controllers/ShippingController.cs`, `UserAddressesController.cs`
- Nova Poshta integration, shipment repositories

## Що готово

- CRUD user addresses, shipping methods, quote calculation.
- Nova Poshta webhook deduplication (container test).
- Fulfillment flow інтегрований з orders (company shipments).

## Blockers (P0)

- ~~`NOVAPOSHTA__ENABLED=false` за замовч.~~ **CLOSED** — prod compose + validator вимагають `NOVAPOSHTA_API_KEY` при увімкненому shipping

## Near-term (P1)

- Container test для повного quote→shipment lifecycle.
- E2E з реальним Nova Poshta sandbox API.

## Checklist

- [x] Domain + Application + API + Infrastructure
- [x] shipping-gate CI
- [x] Webhook dedup container test
- [x] Prod Nova Poshta credentials enforced at startup

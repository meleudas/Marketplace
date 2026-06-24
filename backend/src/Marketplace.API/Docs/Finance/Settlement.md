# Seller settlement & payout

## Overview

Після оплати замовлення система створює `order_financials` (snapshot комісії) та ledger-записи (`Sale`, `PlatformFee`). Кошти тримаються в `Pending` до `AvailableAtUtc` (доставка + `Settlement:HoldDaysAfterDelivery`).

## Формула MVP

- `MerchandiseBase = order.Subtotal - order.DiscountAmount`
- `PlatformFee = round(MerchandiseBase * CommissionPercent / 100, 2)`
- `SellerMerchandiseNet = MerchandiseBase - PlatformFee`
- `SellerPayoutEligible = SellerMerchandiseNet + order.ShippingCost`
- `CommissionPercent` — snapshot з `GetActiveAtAsync(companyId, paymentCompletedAt)`

## Settlement batches

Hangfire job `finance-settlement-batch` (щогодини):

1. Робить eligible ledger entries `Available`
2. Агрегує по `CompanyId` у `SettlementBatch` (`Open` → `Ready`)
3. Метрика `settlement_batch_total{status}`

## Payout

- **Auto:** `finance-seller-payout` при `Settlement:AutoPayoutEnabled=true` → `ISellerPayoutPort` (LiqPay adapter або manual stub)
- **Admin approve:** `POST /admin/settlements/{batchId}/approve-payout`
- **Manual fallback:** `POST /admin/settlements/{batchId}/mark-paid` з `bankReference`

Потрібен `PATCH /companies/{id}/payout-profile` (IBAN, recipient name).

## Seller APIs

| Endpoint | Опис |
|----------|------|
| `GET /companies/{id}/earnings/summary?from&to` | pending / available / settled / fees |
| `GET /companies/{id}/settlements` | batches + payout status |
| `PATCH /companies/{id}/payout-profile` | IBAN для виплат |

## Admin APIs

| Endpoint | Опис |
|----------|------|
| `GET /admin/companies/{id}/commission-rates` | історія ставок |
| `GET /admin/settlements?status&companyId` | черга виплат |
| `POST /admin/settlements/{batchId}/approve-payout` | trigger payout |
| `POST /admin/settlements/{batchId}/mark-paid` | manual complete |

## Refunds

`PaymentRefundExecutor` додає `Refund` ledger entry пропорційно refunded amount.

## Metrics

- `commission_posted_total`
- `seller_ledger_entries_total{entry_type}`
- `seller_payout_total{status}`
- `settlement_batch_total{status}`

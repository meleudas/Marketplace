# Backend Marketplace Checklist

Оновлено на основі поточного стану коду в `backend/src` і `backend/tests`.

Шкала пріоритету:
- `P1` — найвищий (блокує стабільний запуск/продажі)
- `P2` — дуже важливо для production-ready
- `P3` — важливо для якості та масштабування
- `P4` — покращення після стабілізації MVP
- `P5` — nice-to-have

---

## 1) Що вже зроблено повноцінно

### P1
- [x] Checkout flow з розбиттям кошика на замовлення по компаніях (`CheckoutCartCommandHandler`).
- [x] Базовий payment flow: init, webhook, sync, refund (`PaymentsIntegrationsController`, `AdminPaymentsController`, `PaymentJobs`).
- [x] Структура домену для замовлень, платежів, рефандів, cart.
- [x] MinIO інтеграція для upload оригіналів + асинхронна генерація derivative через Hangfire + ImageSharp.
- [x] Elasticsearch пошук з fuzzy по назві (`Fuzziness("AUTO")`).

### P2
- [x] Order Management API v1 (list/detail/status/cancel) для buyer/seller/admin.
- [x] RBAC перевірки доступу для замовлень (`IOrderAccessService`).
- [x] Фільтрація/пагінація замовлень у репозиторії (`OrderListFilter`, `ListAsync`).
- [x] Документація endpoint-ів для Orders у `Docs/Endpoints` і `Docs/ControllerModels`.
- [x] Автотести (82 passed) включно з order/status/access/validator сценаріями.

---

## 2) Що зроблено, але бажано покращити

### P1
- [ ] Інвалідація list-кешів orders при write-операціях зараз неповна (точково чиститься detail-key).
- [ ] Потрібна сильніша узгодженість подій `order-payment-inventory` (idempotency/outbox) для edge-cases.

### P2
- [x] Додано adaptive compression policy в `ProductImageJobs` (guard проти derivative > original через quality-ітерації).
- [x] Додано deterministic cleanup старих MinIO об'єктів при mutate + регулярний orphan cleanup job.
- [x] Додано явний статусний audit trail (`order_status_history`) + timeline у деталях order.

### P3
- [ ] Пошук Elasticsearch зараз частково фільтрується in-memory після вибірки (`Size(1000)`), краще перенести фільтри/сортування в ES query.
- [ ] Потрібні метрики кешу, черг Hangfire, payment/webhook latency та error-rate.

---

## 3) Що ще треба реалізувати (Core)

## P1 (обов'язково найближчим спринтом)
- [ ] Повна cache invalidation стратегія для Orders list/detail (namespace versioning або deterministic key set).
- [ ] Idempotency для ключових write endpoint-ів (`checkout`, `status update`, `cancel`, payment webhook).
- [ ] Failure/retry policy + dead-letter strategy для критичних job/інтеграційних сценаріїв.
- [ ] Конкурентна безпека складу (anti-oversell при high load).

## P2
- [ ] Status history persistence (`order_status_history`) + timeline API.
- [ ] Чітка політика скасування (buyer/seller/admin) з reason codes і SLA.
- [ ] Returns/RMA workflow (request/approve/reject/received/refunded).
- [ ] Shipping/fulfillment API: shipment events, tracking lifecycle, partial shipment readiness.

## P3
- [ ] Розширені seller/admin фільтри orders (status sets, date range, search by order number, сортування, можливо `companyMemberId`).
- [ ] Rate limiting і anti-abuse для auth/checkout/review/payment endpoints.
- [ ] Health/readiness checks для storage/search/queue з алертингом.

---

## 4) Необов'язкові фічі (Optional)

## P3
- [ ] Promotions/coupons engine (ліміти, валідатори, сумісність із кошиком).
- [ ] Notification orchestration (email/sms/telegram templates, retries, templates versioning).

## P4
- [ ] Multi-warehouse order routing і split shipment в одному order-flow.
- [ ] Базовий seller settlement/payout модуль (комісії, звітність).

## P5
- [ ] Recommendations / personalization API.
- [ ] A/B testing infra + feature flag targeting.
- [ ] Loyalty/referral backend.

---

## 5) Рекомендований короткий execution-план (6-8 тижнів)

- `Тиждень 1-2 (P1)`:
  - cache invalidation for orders,
  - idempotency keys,
  - webhook hardening + job retry policy.
- `Тиждень 3-4 (P2)`:
  - order status history table + timeline API,
  - cancel policy + reason codes,
  - media pipeline hardening (adaptive compression + cleanup).
- `Тиждень 5-6 (P2/P3)`:
  - returns/RMA v1,
  - observability (metrics + alerts),
  - security baseline (rate limit + abuse controls).
- `Тиждень 7-8 (P3+)`:
  - search optimization in ES query layer,
  - seller/admin filter expansion,
  - production readiness polishing.

---

## 6) Контрольні критерії "повноцінного" backend маркетплейсу

- [ ] Немає втрати/дубля order/payment подій при retry/webhook replay.
- [ ] Нема oversell під конкурентним навантаженням.
- [ ] Усі статуси замовлень мають історію, причини й аудит хто змінив.
- [ ] Кеш дає стабільний latency boost без stale критичних даних.
- [ ] Storage pipeline обробляє зображення прогнозовано (розмір, quality, cleanup).
- [ ] Є моніторинг + алерти на ключові бізнес-флоу (checkout/payment/order updates).

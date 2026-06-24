# Шлях виконання (роадмап) за документацією `Docs/Notifications`

Цей файл — **порядок робіт** для команди: від уже зробленого до пунктів з [EventCatalog.md](EventCatalog.md), [Channels.md](Channels.md), [Architecture.md](Architecture.md). Після кожного етапу оновлюйте статуси в `EventCatalog.md` (Done / Planned) і при потребі — HTTP-доку в [Endpoints/PushNotifications.md](../Endpoints/PushNotifications.md).

---

## Етап 0 — Узгодити контекст (1 раз)

Прочитати:

- [README.md](README.md) — розділ **«Етап 0 — правило розділення»** та діаграму двох потоків (`INotificationDispatcher` vs `IAppNotificationScheduler`).
- [ConfigurationAndSecurity.md](ConfigurationAndSecurity.md) — VAPID, `.env`, що не класти в push; підрозділ про зв’язок auth vs застосунок.

**Критерій готовності:** кожен розробник розуміє: маркетплейсові події **не** додаються як нові методи в `INotificationDispatcher`; для них — `IAppNotificationScheduler` і канали `INotificationChannel` (у т.ч. майбутні email/Telegram для бізнесу окремо від auth).

### Чеклист для команди (відмітити після онбордингу)

- [ ] Прочитано [README.md — Етап 0](README.md#notifications-stage0)
- [ ] Прочитано [ConfigurationAndSecurity.md](ConfigurationAndSecurity.md)
- [ ] Знаю, де в коді auth-нотифікації: `INotificationDispatcher`, `HangfireNotificationDispatcher`, `NotificationJobs`
- [ ] Знаю, де застосункові: `IAppNotificationScheduler`, `AppNotificationJobs`, `WebPushNotificationChannel`

---

## Етап 1 — Web Push кінець-в-кінець (вже частково зроблено)

Відповідає: [Channels.md](Channels.md) (Web Push), [ServiceWorkerAndWebPush.md](ServiceWorkerAndWebPush.md), [Endpoints/PushNotifications.md](../Endpoints/PushNotifications.md).

| Крок | Дія | Де в коді / доках |
|------|-----|-------------------|
| 1.1 | Згенерувати VAPID, заповнити `WebPush:*`, увімкнути `Enabled` у потрібному середовищі | `backend/.env`, `appsettings`, [ConfigurationAndSecurity.md](ConfigurationAndSecurity.md) |
| 1.2 | Переконатися, що міграція `push_subscriptions` застосована | EF міграції Infrastructure |
| 1.3 | Реалізувати на **фронті** SW + `PushManager.subscribe` + виклики `GET /web-push/vapid-public-key`, `POST/DELETE /me/web-push/subscriptions` | [ServiceWorkerAndWebPush.md](ServiceWorkerAndWebPush.md) — **окремий власник / PR (бекенд-репозиторій фронт не змінюємо тут)** |
| 1.4 | Пройти сценарій: checkout → push адмінам; Shipped/Delivered → push покупцю | [BusinessFlows.md §8](../DDD/BusinessFlows.md), [EventCatalog.md](EventCatalog.md) (рядки Done) |

**Критерії готовності**

- **Бекенд / ops (без фронту):** задані `WEBPUSH__*` і `Enabled`, міграція `push_subscriptions` застосована, Hangfire обробляє `AppNotificationJobs`, при наявності підписок у БД канал Web Push не падає; автотести фіксують виклики `ScheduleAsync` для `AdminNewOrder` (checkout) та `UserOrderStatus` (Shipped/Delivered). Генерація VAPID: `backend/scripts/generate-vapid-keys.ps1` або `npx web-push generate-vapid-keys` (див. кореневий [README.md](../../../../README.md) каталогу `backend`).
- **Повний E2E у браузері:** після виконання кроку **1.3** на клієнті — реальне повідомлення в dev/staging (див. критерій у [Channels.md](Channels.md)).

---

## Етап 2 — In-app нотифікації (заглушка → БД + API)

Відповідає: [Channels.md](Channels.md) (In-app), [Architecture.md](Architecture.md) (персистентний in-app).

| Крок | Дія |
|------|-----|
| 2.1 | Спроєктувати таблицю (узгодити з `Marketplace.Domain/Notifications` та `DB_SCHEMA_POSTGRES_JSONB.md`, якщо актуально) |
| 2.2 | EF: сутність, міграція, репозиторій; замінити no-op у `InAppNotificationChannel` на запис рядка |
| 2.3 | API: список нотифікацій, позначка прочитано (під `[Authorize]`) |
| 2.4 | У `AppNotificationPayloadBuilder` / шаблонах забезпечити стабільні `correlationId` для idempotency на клієнті |

**Критерій готовності:** події з маскою `InApp` з’являються в UI/API без залежності від Web Push.

**Стан:** реалізовано — таблиця `notifications` (міграція `AddNotifications`), канал у Infrastructure, `GET/PATCH` [MeNotifications.md](../Endpoints/MeNotifications.md); у jsonb `data` додано `jobCorrelationId` + `templateKey`; унікальність `(user_id, correlation_id)` у БД; fan-out адмінів через `IAdminNotificationRecipientIds`.

---

## Етап 3 — Розширити події замовлень і оплати

Відповідає: [EventCatalog.md](EventCatalog.md) (розділи «Замовлення та оплата»).

| Крок | Дія |
|------|-----|
| 3.1 | Усі значущі переходи `OrderStatus` → `ScheduleAsync` + нові ключі в `AppNotificationTemplateKeys` + гілки в `AppNotificationPayloadBuilder` |
| 3.2 | Опційно: (а) виклик з `HandleLiqPayWebhook` / sync після зміни платежу, або (б) підписка на outbox `PaymentStatusChanged`, яка лише enqueue Hangfire ([Architecture.md](Architecture.md) — фаза 2) |
| 3.3 | Додати аудиторію «продавець компанії» (не лише `customerId`) — попередньо розширити [RoleAndAudienceMatrix.md](RoleAndAudienceMatrix.md) і модель підписок / топіків |

**Критерій готовності:** рядки в `EventCatalog` з **Planned** для замовлень/оплати переведені на **Done** з посиланням на PR.

**Стан:** виконано (бекенд) — `UpdateOrderStatusCommandHandler` (`Processing` + попередні `Shipped`/`Delivered`), `CancelOrderCommandHandler`, `HandleLiqPayWebhookCommandHandler` (`UserPaymentStatus` для `Completed`/`Failed`/`Refunded`), `CheckoutCartCommandHandler` (`CompanyNewOrder` для `CompanyStakeholders`), аудиторія `CompanyStakeholders` + `TargetCompanyId` у Hangfire-ланцюжку, порти `ICompanyOrderNotificationRecipientIds` / fan-out у `InAppNotificationChannel` та `WebPushNotificationChannel` (Web Push для компанії використовує наявні підписки `UserWebPush` у членів Owner/Manager).

---

## Етап 4 — Кошик і наявність (restock)

Відповідає: [EventCatalog.md](EventCatalog.md) («Каталог і кошик»), [Architecture.md](Architecture.md) (джерело правди).

| Крок | Дія |
|------|-----|
| 4.1 | Вибрати джерело події: зміна `WarehouseStock` / receive / release, outbox inventory-події, або періодичний Hangfire diff |
| 4.2 | Зберігати «watch»: `userId + productId` (таблиця або Redis) при додаванні в кошик або при відображенні OOS |
| 4.3 | При переході OOS → in-stock: `ScheduleAsync` з шаблоном на кшталт `CartProductBackInStock` |
| 4.4 | Rate limit / digest, щоб не спамити при дрібних коливаннях стоку |

**Критерій готовності:** e2e тест або ручний сценарій з документованими кроками в [TestEntitiesAndScenarios.md](../DDD/TestEntitiesAndScenarios.md) (за потреби).

**Стан:** виконано (бекенд) — міграція `AddCartStockWatches`, unit-тести в `ApplicationRestockNotificationsTests` / payload у `ApplicationPushNotificationsTests`; ручний сценарій — у [TestEntitiesAndScenarios.md](../DDD/TestEntitiesAndScenarios.md).

---

## Етап 5 — Товари, модерація, компанійні ролі

Відповідає: [EventCatalog.md](EventCatalog.md) («Товари»), [RoleAndAudienceMatrix.md](RoleAndAudienceMatrix.md).

| Крок | Дія |
|------|-----|
| 5.1 | Продуктове рішення: pending-модерація товару **або** нотифікація owner/manager без зміни статусу товару |
| 5.2 | Домен + API під обраний флоу |
| 5.3 | Розширити `PushSubscriptionAudienceFlags` (або топіки) для moderator / company — міграція БД + оновлення `POST /me/web-push/subscriptions` |
| 5.4 | Події «низький залишок», «схвалено/відхилено» — шаблони + `ScheduleAsync` з відповідних хендлерів |

**Стан (обраний обсяг 5.1–5.2 + нотифікації схвалення/відхилення):** виконано на бекенді — `PendingReview`, `ApproveProduct` / `RejectProduct`, `GET /admin/products/pending` ([Endpoints/AdminProducts.md](../Endpoints/AdminProducts.md)); черга модерації та повідомлення автору — шаблони `AdminProductPendingReview`, `UserProductApproved`, `UserProductRejected`. **5.3** (окремий біт Web Push для модератора) — частково: модератор може використовувати `AdminWebPush` через `POST /me/web-push/subscriptions` з `includeAdminChannel`; окремого `ModeratorWebPush` немає. **5.4** «низький залишок» — див. Етап 4; «схвалено/відхилено» для товарів — зроблено.

---

## Етап 6 — Зовнішні канали Email / Telegram для маркетплейсу

Відповідає: [Channels.md](Channels.md) (Email / Telegram план).

| Крок | Дія |
|------|-----|
| 6.1 | `EmailNotificationChannel` : `INotificationChannel`, виклик `IEmailPort` з шаблонами за `TemplateKey` (окремо від auth-шаблонів) |
| 6.2 | `TelegramAppChannel` : `INotificationChannel`, reuse `ITelegramPort`; політика opt-in і ліміти |
| 6.3 | Розширити `AppNotificationChannelKind` і серіалізацію Hangfire-аргументів узгоджено з існуючими джобами |

**Критерій готовності:** хоча б одна застосункова подія дублюється в email або TG без участі `INotificationDispatcher`.

**Стан:** виконано на бекенді — `AppNotificationChannelKind.Email` / `Telegram`, канали `EmailNotificationChannel` / `TelegramAppChannel` (`IEmailPort` / `ITelegramPort`), `IAppNotificationUserContactReader`, секція `AppNotifications` у конфігурації; приклад події з дублюванням: `UserOrderStatus` у `UpdateOrderStatusCommandHandler` (Push + InApp + Email + Telegram). Прапорці `NotifyAppByEmail` / `NotifyAppByTelegram` на `AspNetUsers` (HTTP PATCH preferences — backlog).

---

## Етап 7 — Надійність і спостережуваність

Відповідає: [Architecture.md](Architecture.md) (outbox фаза 2), [ConfigurationAndSecurity.md](ConfigurationAndSecurity.md) (Hangfire).

| Крок | Дія |
|------|-----|
| 7.1 | За потреби: outbox-подія → лише enqueue Hangfire (без мережі в `OutboxEventProcessor`) |
| 7.2 | Метрики/алерти на зростання помилок `AppNotificationJobs`, dead-letter для нотифікацій (якщо вводиться окрема таблиця) |
| 7.3 | GDPR: налаштування користувача, TTL in-app ([ConfigurationAndSecurity.md](ConfigurationAndSecurity.md)) |

**Стан:** **7.1** — без змін коду, зафіксовано backlog у [Architecture.md](Architecture.md) (outbox лише enqueue). **7.2** — лічильники `hangfire_jobs_total` / `hangfire_job_errors_total` для `app-notification-dispatch` доповнено тегом `template_key`; retry `notification_dispatch` реалізовано в `IntegrationRetryProcessor` + `IAppNotificationRedispatcher`; DLQ метрика `notification_dispatch_dead_letter_total`; Grafana alert на failed channel deliveries (Email/Telegram/SMS). **7.3** — TTL in-app через `AppNotifications:InAppDefaultTtlDays`, щоденна Hangfire-джоба `PruneExpiredInAppNotificationsAsync`, колонки згоди на `AspNetUsers`; API зміни preferences — backlog. **Template versioning (in-code):** `AppNotificationTemplateVersions` + поле `TemplateVersion` у `AppNotificationEnvelope` / in-app `data.templateVersion`. **SMS:** `SmsNotificationChannel`, `AppNotifications:SmsEnabled`, пілот `UserOrderStatus` (Shipped/Delivered).

---

## Порядок пріоритетів (рекомендація)

1. Етап 1 (закрити фронт для Web Push, якщо ще не зроблено).  
2. Етап 2 (in-app — найбільший ефект для «користувач у курсі» без браузерного push).  
3. Етап 3 → 4 → 5 за бізнес-пріоритетом.  
4. Етап 6 і 7 — паралельно або після стабілізації обсягу подій.

Після змін у коді оновлюйте цей роадмап лише якщо змінюється **порядок** або **критерії готовності**; деталі подій лишаються в [EventCatalog.md](EventCatalog.md).

# Domain Report: Notifications

- Статус реалізації: `Implemented`
- Готовність: **85/100**
- Release status: `Conditional`
- Confidence: `High`
- Дата аудиту: 2026-06-29

## Evidence

- CI: `notifications-gate`
- Container: InAppPushPostgresTests, PushSubscriptionPostgresTests

## Межі домену

- `backend/src/Marketplace.Domain/Notifications`
- `backend/src/Marketplace.Application/Notifications`
- `backend/src/Marketplace.API/Controllers/PushNotificationsController.cs`
- `backend/src/Marketplace.API/Controllers/MeNotificationsController.cs`
- `backend/src/Marketplace.Infrastructure/Notifications`
- `backend/src/Marketplace.Infrastructure/Jobs/AppNotificationJobs.cs`
- `backend/src/Marketplace.Infrastructure/Persistence/Repositories/InAppNotificationRepository.cs`
- `backend/src/Marketplace.Infrastructure/Persistence/Repositories/PushSubscriptionRepository.cs`

## Що вже готово

- Багатоканальна архітектура (push/in-app/email/telegram) з feature flags і scheduler через Hangfire.
- Є event catalog і окремий docs-набір по notifications.
- Додано application-тести для list/read handler-ів (`GetMyNotifications`, `MarkNotificationRead`) з negative/ownership сценаріями.
- Додано API-тести для `MeNotificationsController` і `PushNotificationsController` (authz, validation, audience flags, mapping команд).
- Додано SQLite integration/e2e тести для `InAppNotificationRepository` і `PushSubscriptionRepository` (insert/list/mark-read/dedup/delete).
- Додано contract snapshot тести для notifications DTO.
- Додано security regression сценарії для `MeNotifications` (unauthorized + cross-user mark-read).
- Уніфіковано `Suite=Notifications` для профільного notifications test contour і додано окремий CI gate `notifications-gate`.
- Розширено observability: `notification_dispatch_total`, `notification_dispatch_errors_total`, `notification_dispatch_latency_ms`, `notification_channel_deliveries_total`, `notification_channel_errors_total` + runbook alerts.

## Blockers

- Blockers закриті.

## Near-term

- Підтримувати `Suite=Notifications` і розширювати coverage при додаванні нових шаблонів/каналів.
- Винести окремі dashboard-и для channel-level SLI на базі нових `notification_*` метрик.

## Optional

- Додати digest/aggregation режими доставки.
- Додати персональні notification preferences API.
- Еволюціонувати outbox-integration для notifications як платформену фазу (без зміни поточного доменного контракту).

## Мінімальний checklist

- [x] `Push` і `InApp` перевірені інтеграційно.
- [x] `MeNotifications` має authz та ownership coverage.
- [x] Канали мають retry + метрики помилок.
- [x] Є окремий notifications CI quality gate.
- [x] Додано contract coverage для notifications DTO.
- [x] Усі production secrets зберігаються поза репозиторієм.

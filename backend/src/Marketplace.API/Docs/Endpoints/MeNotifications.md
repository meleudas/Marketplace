# MeNotificationsController — `/me/in-app-notifications`

Усі маршрути: **`[Authorize]`** (Bearer).

## `GET /me/in-app-notifications`

- **Призначення:** сторінкований список in-app нотифікацій поточного користувача (таблиця `notifications`).
- **Query:** `page` (int, за замовчуванням `1`), `pageSize` (int, за замовчуванням `20`, максимум `100`).
- **Повертає:** **200** JSON `{ items, total, page, pageSize }`. Кожен елемент `items`: `id`, `templateKey`, `correlationId` (ідемпотентний ключ рядка; для fan-out адмінам відрізняється від `jobCorrelationId` у `data`), `kind`, `title`, `message`, `actionUrl`, `isRead`, `readAt`, `createdAt`, `data` (об’єкт jsonb: поля шаблону + `templateKey` + `jobCorrelationId` для дедупу на клієнті разом із Web Push).
- **Side effects:** немає.

## `PATCH /me/in-app-notifications/{id}/read`

- **Призначення:** позначити нотифікацію прочитаною (лише власник рядка).
- **Path:** `id` — bigint.
- **Повертає:** **200** без тіла при успіху.
- **Помилки:** **404** якщо рядок не знайдено або не належить користувачу (`detail` містить `not found`).

## Зв’язок з доставкою

Записи з’являються, коли Hangfire виконує `AppNotificationJobs` і канал `InAppNotificationChannel` зберігає рядок (див. [Notifications/Channels.md](../Notifications/Channels.md)). Міграція: `AddNotifications` (`notifications`).

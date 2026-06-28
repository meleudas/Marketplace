# MeNotificationsController — `/me/in-app-notifications`

Усі маршрути: **`[Authorize]`** (Bearer).

## `GET /me/in-app-notifications`

- **Summary (1 рядок):** Сторінкований список in-app нотифікацій.
- **Призначення:** сторінкований список in-app нотифікацій поточного користувача (таблиця `notifications`).
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: будь-який автентифікований користувач
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Визначити `userId` з JWT
  2. Завантажити сторінку нотифікацій (`page`, `pageSize`)
  3. Повернути `{ items, total, page, pageSize }`
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Notifications bell
  - API-модуль: —
  - Статус: `planned`
- **Query:** `page` (int, за замовчуванням `1`), `pageSize` (int, за замовчуванням `20`, максимум `100`).
- **Повертає:** **200** JSON `{ items, total, page, pageSize }`. Кожен елемент `items`: `id`, `templateKey`, `correlationId` (ідемпотентний ключ рядка; для fan-out адмінам відрізняється від `jobCorrelationId` у `data`), `kind`, `title`, `message`, `actionUrl`, `isRead`, `readAt`, `createdAt`, `data` (об'єкт jsonb: поля шаблону + `templateKey` + `jobCorrelationId` для дедупу на клієнті разом із Web Push).

## `PATCH /me/in-app-notifications/{id}/read`

- **Summary (1 рядок):** Позначити нотифікацію прочитаною.
- **Призначення:** позначити нотифікацію прочитаною (лише власник рядка).
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: власник нотифікації
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти нотифікацію за `id` (лише власник)
  2. Встановити `isRead = true`, `readAt`
- **Side effects (синхронно):** оновлення рядка `notifications`
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Notifications bell (planned)
  - API-модуль: —
  - Статус: `planned`
- **Path:** `id` — bigint.
- **Повертає:** **200** без тіла при успіху.
- **Помилки:** **404** якщо рядок не знайдено або не належить користувачу (`detail` містить `not found`).

## Зв'язок з доставкою

Записи з'являються, коли Hangfire виконує `AppNotificationJobs` і канал `InAppNotificationChannel` зберігає рядок (див. [Notifications/Channels.md](../Notifications/Channels.md)). Міграція: `AddNotifications` (`notifications`).

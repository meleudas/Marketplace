# AdminOutboxController — `/admin/outbox`

Усі маршрути: **`[Authorize(Roles = "Admin")]`**.

### `GET /admin/outbox/dead-letters`

- **Summary:** Сторінковий список dead-letter outbox-повідомлень.
- **Query:** `page`, `pageSize` (max 100)
- **Повертає:** `{ items[], total, page, pageSize }` з `OutboxMessageAdminDto`

### `GET /admin/outbox/stuck`

- **Summary:** Outbox-повідомлення з простроченим `next_attempt` (failed, не DLQ, >15 хв).
- **Query:** `page`, `pageSize`
- **Повертає:** пагінований список для ops triage

### `POST /admin/outbox/{messageId}/requeue`

- **Summary (1 рядок):** Повторна постановка dead-letter outbox-повідомлення в чергу.
- **Призначення:** повернути dead-letter outbox message назад у pending-обробку.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Перевірити роль Admin
  2. Знайти outbox message за `messageId`
  3. Очистити dead-letter поля, скинути `attempts`, встановити `next_attempt_at`
- **Side effects (синхронно):** очищує `dead_letter_*`, скидає `attempts`, ставить `next_attempt_at` для повторної dispatch-обробки
- **Async / «магія»:** Hangfire job `outbox-dispatch` підхопить повідомлення
- **Де на фронті:**
  - Екран: AdminShell / outbox management (planned)
  - API-модуль: —
  - Статус: `partial`
- **Приймає:** path `messageId` (GUID).
- **Повертає:** **200** порожнє тіло.
- **Помилки:** без `sub` → **401**, non-admin → **403**
- **Metrics:** `outbox_dispatch_total`, `outbox_dispatch_errors_total`, `outbox_dead_letter_total`, `hangfire_job_*{job="outbox-dispatch"}`.

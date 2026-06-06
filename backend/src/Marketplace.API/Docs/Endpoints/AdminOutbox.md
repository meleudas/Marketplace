# AdminOutboxController — `/admin/outbox`

Усі маршрути: **`[Authorize(Roles = "Admin")]`**.

### `POST /admin/outbox/{messageId}/requeue`

- **Призначення:** повернути dead-letter outbox message назад у pending-обробку.
- **Приймає:** path `messageId` (GUID).
- **Повертає:** **200** порожнє тіло.
- **Авторизація:** Admin JWT (без `sub` -> **401**, non-admin -> **403**).
- **Side effects:** очищує `dead_letter_*`, скидає `attempts`, ставить `next_attempt_at` для повторної dispatch-обробки.
- **Metrics:** `outbox_dispatch_total`, `outbox_dispatch_errors_total`, `outbox_dead_letter_total`, `hangfire_job_*{job="outbox-dispatch"}`.

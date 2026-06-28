# PushNotificationsController — Web Push (VAPID)

Web Push: публічний ключ для підписки в браузері та збереження/видалення підписок у БД (`push_subscriptions`). Відправка повідомлень з бекенду йде через **Hangfire** (`AppNotificationJobs`) і не використовує auth-інтерфейс `INotificationDispatcher` (лише email/Telegram/SMS).

| Маршрут | Призначення |
|---------|-------------|
| `GET /web-push/vapid-public-key` | Публічний VAPID-ключ (без авторизації) |
| `POST /me/web-push/subscriptions` | Зареєструвати/оновити підписку браузера |
| `DELETE /me/web-push/subscriptions` | Видалити підписку за `endpoint` |

## `GET /web-push/vapid-public-key`

- **Summary (1 рядок):** Публічний VAPID-ключ для браузерної підписки.
- **Призначення:** віддати клієнту **публічний** VAPID-ключ і `subject` для `PushManager.subscribe` у Service Worker.
- **Хто може викликати:**
  - JWT: не потрібна
  - Глобальні ролі: —
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Зчитати `publicKey` та `subject` з конфігурації `WebPush`
  2. Повернути `VapidPublicKeyResponse`
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Push opt-in / Service Worker setup (planned)
  - API-модуль: —
  - Статус: `planned`
- **Приймає:** немає.
- **Повертає:** **200** JSON `VapidPublicKeyResponse`: `publicKey`, `subject` (з конфігурації `WebPush`).
- **Примітки:** якщо ключі ще не задані в конфігурації, `publicKey` може бути порожнім рядком — клієнт не зможе коректно підписатися, доки не налаштують `WEBPUSH__PUBLICKEY` / `WEBPUSH__PRIVATEKEY`.

## `POST /me/web-push/subscriptions`

- **Summary (1 рядок):** Реєстрація або оновлення Web Push підписки браузера.
- **Призначення:** upsert підписки браузера (endpoint + ключі шифрування) для поточного користувача (`sub` з JWT).
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: будь-який автентифікований; `includeAdminChannel` — лише **Admin** / **Moderator**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Валідувати `endpoint`, `p256dh`, `auth`
  2. Обчислити прапорці аудиторії (`UserWebPush`, `AdminWebPush`)
  3. Upsert рядок у `push_subscriptions` (унікальність по `endpoint`)
- **Side effects (синхронно):** запис/оновлення рядка в `push_subscriptions` (при зміні власника endpoint старий рядок видаляється)
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Push opt-in
  - API-модуль: —
  - Статус: `planned`
- **Приймає:** body `RegisterPushSubscriptionRequest` (camelCase):
  - `endpoint` (string, обов'язково),
  - `p256dh` (string, обов'язково),
  - `auth` (string, обов'язково),
  - `includeUserChannel` (bool, за замовчуванням `true`) — прапорець аудиторії **покупець/користувач** (`UserWebPush`),
  - `includeAdminChannel` (bool, за замовчуванням `false`) — прапорець аудиторії **адмін/модератор** (`AdminWebPush`); **ігнорується**, якщо у JWT немає ролі **Admin** або **Moderator** (глобальні ролі платформи).
- **Повертає:** **204 No Content** при успіху.
- **Помилки:** **401** без user id; **400** якщо не передано `endpoint` / `p256dh` / `auth`, або якщо після обчислення прапорців аудиторії не залишилось жодного (наприклад, лише `includeAdminChannel=true` для користувача без ролей **Admin** / **Moderator**).
- **Примітки:** `User-Agent` запиту (якщо є) зберігається обрізаним до 512 символів.

## `DELETE /me/web-push/subscriptions`

- **Summary (1 рядок):** Видалення Web Push підписки за endpoint.
- **Призначення:** видалити підписку поточного користувача за `endpoint`.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: власник підписки
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Валідувати query `endpoint`
  2. Видалити рядок за парою `(userId, endpoint)`
- **Side effects (синхронно):** видалення з `push_subscriptions`
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Push opt-out / settings (planned)
  - API-модуль: —
  - Статус: `planned`
- **Приймає:** query `endpoint` (string, обов'язково).
- **Повертає:** **204 No Content** (у тому числі якщо рядка не було — операція idempotent за змістом для клієнта).
- **Помилки:** **401**; **400** без `endpoint`.

## Конфігурація та поведінка доставки

- Секція **`WebPush`** у `appsettings` / змінні середовища `WEBPUSH__*` (див. [README.md](../../../../README.md) репозиторію в каталозі `backend`, розділ Web Push). Підписки зберігаються в таблиці **`push_subscriptions`** після міграції `20260510175257_AddPushSubscriptions`.
- Канал Web Push **не** викликає мережу, якщо `WebPush:Enabled` = `false` або не задані публічний/приватний ключі; канал **in-app** у джобі лишається заглушкою для майбутньої реалізації.
- Відповідь push-сервісу **410 Gone** призводить до видалення мертвої підписки з БД.

## Пов'язані бізнес-події (не HTTP)

- Після успішного checkout для кожного створеного замовлення ставиться в чергу Hangfire нотифікація шаблону **`AdminNewOrder`** (аудиторія адмін-підписок).
- Після переходу замовлення в **`Shipped`** або **`Delivered`** — **`UserOrderStatus`** покупцю (`customerId`).

Детальніше: [BusinessFlows.md](../DDD/BusinessFlows.md) (розділ про Web Push).

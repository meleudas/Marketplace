# Service Worker і Web Push (клієнт)

## Реалізовано на бекенді

- `GET /web-push/vapid-public-key` — публічний ключ + `subject` для `applicationServerKey`.
- `POST /me/web-push/subscriptions` / `DELETE /me/web-push/subscriptions` — збереження та видалення підписки (Bearer).

Повний опис: [Endpoints/PushNotifications.md](../Endpoints/PushNotifications.md), приклади body: [ControllerModels/PushNotificationsController.md](../ControllerModels/PushNotificationsController.md).

## План: Next.js і Service Worker

**Власник:** фронтенд / SPA (окремий PR). Бекенд надає лише HTTP-контракт вище; зміни в репозиторії фронту не є частиною бекенд-only дорожньої карти Web Push.

Нижче — **цільовий** клієнтський флоу; репозиторій може ще не містити SW-файлів.

### 1. Реєстрація Service Worker

- Файл SW (наприклад `public/sw.js` або згенерований бандл) має обробляти:
  - `push` — отримати `event.data.json()` або текст; показати `self.registration.showNotification(title, options)` з `data.url` для deep link.
  - `notificationclick` — `event.notification.close()`, `clients.openWindow(url)` або фокус існуючої вкладки.
- Зареєструвати SW після логіну (або на сторінці налаштувань нотифікацій), перевіряючи `serviceWorker` in navigator і `Notification.permission`.

### 2. Підписка на push

1. `GET /web-push/vapid-public-key` → `publicKey`, `subject`.
2. `PushManager.subscribe({ userVisibleOnly: true, applicationServerKey: urlBase64ToUint8Array(publicKey) })`.
3. `POST /me/web-push/subscriptions` з тілом `endpoint`, `p256dh`, `auth` з `subscription.getKey()` (base64url), прапорцями `includeUserChannel` / `includeAdminChannel` (адмін-прапорець на сервері валідується роллю `Admin`).

### 3. Узгодження з payload бекенду

Бекенд надсилає JSON-рядок виду:

`{ "title", "body", "url", "tag" }` (див. `WebPushNotificationChannel`).

- `tag` зараз = `correlationId` (hex без дефісів) — можна використовувати для заміни попередніх повідомлень одного типу на клієнті.
- `url` — deep link з `Frontend:BaseUrl` + шлях (`/orders/{id}`, `/admin/orders/{id}` тощо).

### 4. Обмеження та безпека

- **HTTPS** (або localhost) для Web Push.
- Дозвіл браузера `Notification` — користувач має явно погодитись.
- Не покладатися на push для критичних юридичних фактів без дублювання (email / in-app історія).

# PushNotificationsController — приклади JSON

Повний опис маршрутів: [Endpoints/PushNotifications.md](../Endpoints/PushNotifications.md).

## `POST /me/web-push/subscriptions`

Мінімальний запит (користувацький канал увімкнено за замовчуванням):

```json
{
  "endpoint": "https://fcm.googleapis.com/fcm/send/...",
  "p256dh": "BNc...",
  "auth": "tBH..."
}
```

Покупець + адмін (лише якщо JWT з роллю `Admin`; інакше `includeAdminChannel` не дасть прапорець адміна):

```json
{
  "endpoint": "https://updates.push.services.mozilla.com/wpush/v2/...",
  "p256dh": "BKx...",
  "auth": "x8y...",
  "includeUserChannel": true,
  "includeAdminChannel": true
}
```

Тільки адмін-сповіщення на тому ж браузері (для користувача з роллю Admin):

```json
{
  "endpoint": "https://example.push.endpoint/...",
  "p256dh": "B...",
  "auth": "a...",
  "includeUserChannel": false,
  "includeAdminChannel": true
}
```

## `DELETE /me/web-push/subscriptions`

Параметр query (URL-encoded), наприклад:

`/me/web-push/subscriptions?endpoint=https%3A%2F%2Ffcm.googleapis.com%2Ffcm%2Fsend%2F...`

# Конфігурація та безпека нотифікацій

## Зв’язок з двома потоками нотифікацій

- **VAPID / Web Push / `push_subscriptions`** стосуються лише застосункового пайплайна (`IAppNotificationScheduler`, канал Web Push). Див. [README.md — Етап 0](README.md#notifications-stage0).
- **SendGrid / Telegram-бот для 2FA** — це секрети й політики **auth**; вони не «перетворюються» на маркетплейсові розсилки через додавання методів у `INotificationDispatcher`. Маркетплейсові листи/TG (коли з’являться) — окремі шаблони й виклики з каналів застосунку ([Channels.md](Channels.md)).

## Змінні середовища та appsettings

| Ключ | Призначення |
|------|--------------|
| `WebPush:Enabled` | Увімкнути мережеву відправку Web Push (інакше канал тихо пропускає HTTP). |
| `WebPush:Subject` | VAPID subject (`mailto:...` або `https://...`) для push-провайдерів. |
| `WebPush:PublicKey` / `WebPush:PrivateKey` | Пара VAPID; приватний ключ — **секрет**. |
| `Frontend:BaseUrl` | База для deep link у payload (`AppNotificationPayloadBuilder`). |
| `AppNotifications:EmailEnabled` | Увімкнути канал застосункових листів (`EmailNotificationChannel`). |
| `AppNotifications:TelegramEnabled` | Увімкнути канал застосункового Telegram (`TelegramAppChannel`). |
| `AppNotifications:InAppDefaultTtlDays` | TTL для нових in-app рядків; `0` — без `ExpiresAt`. |
| `AppNotifications:EmailSubjectPrefix` | Префікс subject для маркетплейс-листів (відмінно від auth). |
| `AppNotifications:PruneExpiredInAppEnabled` | Щоденна Hangfire-очистка прострочених in-app. |

Приклад змінних: `backend/.env.example` (`WEBPUSH__*`, `APPNOTIFICATIONS__*`).

Генерація ключів: див. розділ **Web Push** у кореневому [README.md](../../../../README.md) проєкту `backend`.

## Що не класти в push payload

- ПІБ, повна адреса, телефон, повний email у відкритому вигляді.
- Достатньо: короткий title/body, ідентифікатори сутностей (`orderId`), `url` на SPA для деталей після авторизації.

## Авторизація API підписок

- `GET /web-push/vapid-public-key` — публічно (лише публічний ключ).
- `POST` / `DELETE` **`/me/web-push/subscriptions`** — лише Bearer; прапорець **admin-каналу** підписки (`includeAdminChannel`) — для ролей `Admin` або `Moderator` (див. [PushNotifications.md](../Endpoints/PushNotifications.md)).

## GDPR / згода

- **Transactional застосункові канали:** на `AspNetUsers` зберігаються `NotifyAppByEmail` та `NotifyAppByTelegram` (дефолт `true`). Листи йдуть лише на **підтверджений** email; Telegram — лише за наявності `TelegramChatId`. Маркетингові розсилки та HTTP API зміни цих прапорців — окремий PR (backlog).
- **In-app TTL:** див. `AppNotifications:InAppDefaultTtlDays` і щоденне prune за `ExpiresAt` ([Channels.md](Channels.md)).

## Hangfire

- Dashboard: `/hangfire` (захист у production окремо від документації API).
- Джоба `AppNotificationJobs` має retry; помилки логуються та в метриках `hangfire_job_*` (для `app-notification-dispatch` додано тег `template_key`).

## Розділення секретів

- Auth SendGrid / Telegram — не змішувати з VAPID; різні ротації ключів і різні політики доступу в CI/CD.

## Staging і CI/CD

- У середовищі **staging/production** задавайте `WEBPUSH__*` через secret store (GitHub Actions secrets, Azure Key Vault тощо), а не файлами в репозиторії.
- Після деплою перевірте **`GET /web-push/vapid-public-key`** (публічний ключ і `subject`) і наявність таблиці **`push_subscriptions`** у БД після застосування міграцій.

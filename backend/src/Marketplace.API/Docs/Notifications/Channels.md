# Канали доставки нотифікацій

Таблиця: **канал** | **стан** | **Примітки та файли**

## Web Push (браузер)

| Аспект | Деталі |
|--------|--------|
| **Статус** | Реалізовано (відправка залежить від `WebPush:Enabled` і наявності VAPID-ключів). |
| **Код** | `WebPushNotificationChannel`, `LibWebPushDeliveryClient` (`Lib.Net.Http.WebPush`), `PushSubscriptionRepository`, `PushNotificationsController`. |
| **Поведінка** | Підписки зберігаються в `push_subscriptions`; відповідь **410 Gone** → видалення підписки. Payload — легкий JSON (title, body, url, tag), без PII. Для `AppNotificationAudienceKind.CompanyStakeholders` канал збирає підписки `UserWebPush` у кожного Owner/Manager компанії й дедуплікує за `endpoint`. Окремого прапорця `CompanyWebPush` немає — ті самі браузерні підписки користувача. |
| **Міграція** | Таблиця створюється міграцією `20260510175257_AddPushSubscriptions`; застосування та перевірка — у розділі **Web Push** кореневого [README.md](../../../../README.md) каталогу `backend`. |

Див. також [ServiceWorkerAndWebPush.md](ServiceWorkerAndWebPush.md) та [ConfigurationAndSecurity.md](ConfigurationAndSecurity.md).

## In-app (у додатку / список сповіщень)

| Аспект | Деталі |
|--------|--------|
| **Статус** | Реалізовано: запис у таблицю `notifications`, API [MeNotifications.md](../Endpoints/MeNotifications.md). |
| **Код** | `InAppNotificationChannel` (Infrastructure), `IInAppNotificationRepository` / `InAppNotificationRepository`, `IAdminNotificationRecipientIds` (fan-out для `AppNotificationAudienceKind.Admins`), `ICompanyOrderNotificationRecipientIds` (fan-out для `CompanyStakeholders`), `MeNotificationsController`. |
| **Поведінка** | У `data` (jsonb) зберігаються вихідні поля payload, `templateKey` та `jobCorrelationId` (оригінальний `CorrelationId` джоби) для клієнтського дедупу; у БД колонка `CorrelationId` унікальна разом із `user_id` (для адмінів і fan-out компанії — детермінований похідний UUID на користувача, див. `InAppNotificationCorrelation.PerUser`). `ExpiresAt` заповнюється з `AppNotifications:InAppDefaultTtlDays` (0 = без терміну). |
| **Міграція** | `AddNotifications` (`20260510183605_AddNotifications`). |
| **Домен** | `Marketplace.Domain/Notifications/Entities/Notification.cs` — `Create`, `MarkAsRead` (для майбутнього використання; персистентний шлях через EF-запис). |

## Email (транзакційні застосункові листи)

| Аспект | Деталі |
|--------|--------|
| **Статус (auth)** | Реалізовано: `IEmailPort`, SendGrid / logging, виклики з `INotificationDispatcher` / Hangfire `NotificationJobs`. |
| **Статус (маркетплейс)** | `EmailNotificationChannel` : `INotificationChannel` → `IEmailPort.SendEmailAsync` з subject/body з `AppNotificationEnvelope` (префікс `AppNotifications:EmailSubjectPrefix`). Одержувачі: `IAppNotificationUserContactReader` (email підтверджений + `NotifyAppByEmail` на `AspNetUsers`), fan-out для `Admins` / `CompanyStakeholders` як у in-app. Вимикач: `AppNotifications:EmailEnabled`. |

## Telegram (застосункові події)

| Аспект | Деталі |
|--------|--------|
| **Статус (auth / 2FA)** | Реалізовано: `ITelegramPort`, `EnqueueTelegramMessageAsync` у auth-диспетчері. |
| **Статус (маркетплейс)** | `TelegramAppChannel` : `INotificationChannel` → `ITelegramPort.SendMessageAsync`; текст з envelope (truncate до ліміту). Лише якщо є `TelegramChatId` і `NotifyAppByTelegram=true`. Вимикач: `AppNotifications:TelegramEnabled`. |

## SMS

| Аспект | Деталі |
|--------|--------|
| **Статус** | Auth через `ISmsPort` / Hangfire. |
| **План** | За потреби — окремий канал для маркетплейсу (рідко для замовлень через вартість). |

## Маска каналів у запиті

`AppNotificationChannelKind` — прапорці `Push` (1), `InApp` (2), `Email` (4), `Telegram` (8); Hangfire передає маску як `int` у `AppNotificationJobs.DispatchAsync` (сумісність із старими джобами в черзі).

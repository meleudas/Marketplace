# Архітектура застосункових нотифікацій

## Реалізовано зараз

### Шари

| Шар | Відповідальність |
|-----|-------------------|
| **Application** | `IAppNotificationScheduler`, `AppNotificationRequest` / `AppNotificationEnvelope`, `INotificationChannel`, `IInAppNotificationRepository`, `IAdminNotificationRecipientIds`, `IPushSubscriptionRepository`, `IPushDeliveryClient`, константи шаблонів `AppNotificationTemplateKeys`, MediatR-запити для списку/прочитання in-app. |
| **Infrastructure** | `HangfireAppNotificationScheduler`, `AppNotificationJobs`, `WebPushNotificationChannel`, `InAppNotificationChannel`, `EmailNotificationChannel`, `TelegramAppChannel`, `LibWebPushDeliveryClient`, `AppNotificationPayloadBuilder`, `IAppNotificationUserContactReader`, репозиторії підписок і `notifications`, `WebPushOptions`, `AppNotificationOptions`. |
| **API** | `PushNotificationsController` — Web Push; `MeNotificationsController` — in-app. Деталі: [PushNotifications.md](../Endpoints/PushNotifications.md), [MeNotifications.md](../Endpoints/MeNotifications.md). |

### Життєвий цикл

1. Командний хендлер (наприклад checkout або зміна статусу замовлення) викликає `IAppNotificationScheduler.ScheduleAsync(...)`.
2. Hangfire серіалізує виклик `AppNotificationJobs.DispatchAsync(...)` з примітивними аргументами (стійко до серіалізації).
3. Джоба будує `AppNotificationEnvelope` через `AppNotificationPayloadBuilder` і для кожного зареєстрованого `INotificationChannel`, чий `Kind` входить у маску каналів, викликає `DeliverAsync`.

### Fail/skip semantics (runtime)

- `AppNotificationJobs.DispatchAsync` має `AutomaticRetry(Attempts = 3)` на рівні Hangfire job.
- Якщо будь-який канал кидає виняток, job вважається failed і піде на retry (щоб не втрачати доставку через тимчасовий збій каналу).
- Якщо канал свідомо не доставляє повідомлення без винятку (feature flag off, немає отримувачів, user opted-out), це трактується як **skip**, а не failure.
- Для observability доступні доменні метрики dispatch/channels: `notification_dispatch_total`, `notification_dispatch_errors_total`, `notification_dispatch_latency_ms`, `notification_channel_deliveries_total`, `notification_channel_errors_total`.

### Розширення

- Новий канал доставки = нова реалізація `INotificationChannel` + реєстрація через `services.TryAddEnumerable(ServiceDescriptor.Scoped<INotificationChannel, ...>)` у `Marketplace.Infrastructure` `DependencyInjection` (поряд з існуючими каналами).
- Новий шаблон = нова гілка у `AppNotificationPayloadBuilder.Build` + константа в `AppNotificationTemplateKeys` + виклик `ScheduleAsync` з потрібного хендлера.

## План / дизайн (не імплементовано як єдиний стандарт)

### Outbox + Hangfire (фаза 2)

- Критичні доменні події вже пишуться в `outbox_messages` з типами, що обробляє `OutboxEventProcessor`. **План (Етап 7.1, backlog):** додати тип події на кшталт `AppNotificationRequested` або розширити існуючі гілки так, щоб процесор лише **enqueue** Hangfire (без мережі в циклі outbox), якщо потрібна та сама атомарність транзакції, що й для інших side effects. **Стан:** не реалізовано — застосункові нотифікації як і раніше ставляться з MediatR-хендлерів через `IAppNotificationScheduler` після збереження; outbox-процесор не викликає `AppNotificationJobs`.
- Зараз застосункові нотифікації ставляться з хендлерів **після** збереження змін у тій самій одиниці роботи, що й outbox для замовлень — це простіше, але при збої між commit і enqueue теоретично можлива втрата задачі; документувати цей компроміс для команди.

### Узгодження з MediatR pipeline

- **План:** опційно централізувати публікацію через pipeline behavior після успішного `SaveChanges`, щоб не дублювати виклики в кожному хендлері — оцінити складність і тестованість.

## Зв’язок з auth-нотифікаціями

| Компонент | Призначення |
|-----------|-------------|
| `INotificationDispatcher` | Лише сценарії акаунта: підтвердження email, скидання пароля, 2FA, auth-Telegram/SMS. |
| `IAppNotificationScheduler` | Події маркетплейсу: замовлення, (майбутнє) каталог, кошик, модерація. |

Не розширювати `INotificationDispatcher` методами Web Push або in-app — уникнути змішування доменів.

# Ролі, аудиторії та канали

Базова матриця доступів до **HTTP API**: [RoleAccessMatrix.md](../DDD/RoleAccessMatrix.md).  
Тут — **хто що отримує** у вигляді нотифікацій (цільова модель + обмеження поточної реалізації Web Push).

## Глобальні ролі (`UserRole`)

| Роль | Типові нотифікації (ціль) | Web Push зараз |
|------|---------------------------|----------------|
| **Buyer** (і типово **Seller** як покупець) | Замовлення, кошик, відгуки, промо (обережно) | Підписка з `UserWebPush` після `POST /me/web-push/subscriptions` з `includeUserChannel`. |
| **Seller** (глобальний маркер) | Те саме + (план) події компанії, якщо не покриті компанійними ролями | За потреби окремі audience flags у майбутньому (див. нижче). |
| **Moderator** | Черга модерації (відгуки, **товари** — InApp/Push через ту саму аудиторію `Admins` і `IAdminNotificationRecipientIds`) | **Частково:** той самий `AdminWebPush`, що й у адміна, якщо JWT з роллю `Moderator` і `includeAdminChannel: true` у `POST /me/web-push/subscriptions`. Окремий `ModeratorWebPush` — **план** (Етап 5.3). |
| **Admin** | Платформа: нові замовлення, модерація товарів/відгуків, системні алерти | `AdminWebPush` якщо JWT з роллю `Admin` і `includeAdminChannel: true`. |

## Компанійні ролі (`CompanyMembershipRole`)

| Роль | Типові нотифікації (ціль) | Примітка |
|------|---------------------------|----------|
| **Owner / Manager** | Замовлення по компанії (нове замовлення з checkout — шаблон `CompanyNewOrder`), інвентар, низький сток, скарги | **Частково зроблено:** `AppNotificationAudienceKind.CompanyStakeholders` + `TargetCompanyId`; отримувачі — `ICompanyOrderNotificationRecipientIds` / `CompanyOrderNotificationRecipientIds` (лише Owner/Manager, не видалені). Інші події замовлення (зміна статусу тощо) для компанії — за потреби окремий PR за [EventCatalog.md](EventCatalog.md). |
| **Seller** | Власні товари / замовлення по лінії (за продуктом) | План. |
| **Support / Logistics** | Обмежений набір (доставка, повернення) | План; узгодити з матрицею доступів до inventory. |

## Поточні обмеження `PushSubscriptionAudienceFlags`

У коді зараз лише:

- `UserWebPush` — клієнтські сповіщення.
- `AdminWebPush` — сповіщення для глобальних адмінів.

**План розширення:** додати бітові прапорці або окрему таблицю «топіки» (`company:{id}:orders`, `role:moderator`) без зламання існуючих підписок (міграція з дефолтом).

## Канали за типом ролі (ціль)

| Канал | Хто типово | Opt-in |
|-------|------------|--------|
| Push | Усі, хто підписав браузер | Так (браузер + API) |
| In-app | Автентифікований користувач у застосунку | Так (налаштування профілю) |
| Email | За email у профілі | Політика розсилки + unsubscribe |
| Telegram | Лише якщо прив’язаний чат (як для 2FA або окреме поле) | Так |

## Anti-spam

- **План:** rate limit на шаблон + dedup по `correlationId` для повторних Hangfire retries; для адмінів — digest замість кожної дрібниці.

# Каталог подій нотифікацій

Колонки: **Подія** | **Аудиторія** | **Канали (ціль)** | **Статус**

Статус: **Done** — є виклик `IAppNotificationScheduler` / канал; **Planned** — продуктовий або технічний план без повної імплементації.

---

## Замовлення та оплата

| Подія | Аудиторія | Канали (ціль) | Статус |
|--------|-----------|---------------|--------|
| Нове замовлення після checkout | Глобальні адміни (`AdminWebPush`); Owner/Manager компанії замовлення (`AppNotificationAudienceKind.CompanyStakeholders`, шаблон `CompanyNewOrder`) | Push + InApp + (план) Email/TG | **Done** — `CheckoutCartCommandHandler` (`AdminNewOrder` + `CompanyNewOrder`; кореляція компанії: `AppNotificationCorrelationIds.Deterministic("checkout-company|…")`). |
| Статус замовлення → `Processing`, `Shipped`, `Delivered` | Покупець (`customerId`) | Push + InApp + (план) Email | **Done** — шаблон `UserOrderStatus`, `UpdateOrderStatusCommandHandler`. |
| Скасування замовлення (`Cancelled`) | Покупець | Push + InApp + (план) Email | **Done** — `UserOrderStatus` з `status=Cancelled`, `CancelOrderCommandHandler`. |
| Платіж `Completed` / `Failed` / `Refunded` (LiqPay webhook) | Покупець | Push + InApp + (план) Email | **Done** — шаблон `UserPaymentStatus`, `HandleLiqPayWebhookCommandHandler` після оновлення замовлення; стабільний `CorrelationId`: `AppNotificationCorrelationIds.PaymentBuyerNotify(transactionId, paymentStatus)` (компроміс (а) з [ImplementationRoadmap.md](ImplementationRoadmap.md) — не outbox-consumer). |

---

## Каталог і кошик

| Подія | Аудиторія | Канали (ціль) | Статус |
|--------|-----------|---------------|--------|
| Товар у кошику був недоступний, з’явився сток | Користувач-власник кошика | Push, InApp, (план) Email digest | **Done** — таблиця `cart_stock_watches` (`ICartStockWatchRepository`), реєстрація при `cartQty > Sum(Available)` у `AddCartItem` / `UpdateCartItemQuantity`, зняття при `RemoveCartItem` / `ClearCart` / `CheckoutCart` (`CartStockWatchSyncService`); тригер `IRestockAvailabilityNotifier` після зміни сукупного `Available` з 0 на >0 у `ReceiveStockCommandHandler`, `ReleaseReservationCommandHandler`, `AdjustStockCommandHandler`, `TransferStockCommandHandler` (синхронно після збереження, див. [Architecture.md](Architecture.md)); шаблон `CartProductBackInStock`; `CorrelationId` = `AppNotificationCorrelationIds.CartRestockNotify`; rate limit **24h** через `LastNotifiedAtUtc`. **Digest** кількох SKU в одне повідомлення — залишається **Planned**. |
| Зміна ціни товару в кошику | Власник кошика | InApp (опційно Push) | **Planned**. |
| Абандонed cart | Власник кошика | Email | **Planned** — маркетинг; окремо від транзакційних. |

---

## Товари та модерація

| Подія | Аудиторія | Канали (ціль) | Статус |
|--------|-----------|---------------|--------|
| «Запит на створення товару» на схвалення адміну | Адміни / модератори | InApp, Push, (план) TG | **Done** (бекенд) — `ProductStatus.PendingReview`; після `CreateProductCommandHandler` / повторної подачі з `UpdateProductCommandHandler` (чернетка після відхилення) — `ScheduleAsync` з шаблоном `AdminProductPendingReview`, аудиторія `Admins`, отримувачі `IAdminNotificationRecipientIds` (Admin + Moderator); стабільний `CorrelationId` = `AppNotificationCorrelationIds.ProductPendingReviewQueue` для create/resubmit. |
| Товар схвалено / відхилено | Автор подання (`SubmittedByUserId`) | InApp, Push | **Done** (бекенд, без Email) — `ApproveProductCommandHandler` / `RejectProductCommandHandler` → `UserProductApproved` / `UserProductRejected`; Email залишається **Planned**. |
| Низький залишок (мінімальний поріг) | Owner/Manager компанії | InApp, Email | **Planned** — зв’язок з `minStock` і каталогом. |

---

## Відгуки та підтримка (приклади)

| Подія | Аудиторія | Канали | Статус |
|--------|-----------|--------|--------|
| Відгук на модерації | Модератор / адмін | InApp, Push | **Planned** — узгодити з `AdminReviewsController`. |
| Відповідь продавця на відгук покупця | Покупець | InApp, Push, Email | **Planned**. |

---

## Узгодження шаблонів

Нові події додають константу в `AppNotificationTemplateKeys` і гілку в `AppNotificationPayloadBuilder` (або стратегію за ключем), щоб title/body/url залишались централізованими та перекладабельними (майбутній i18n).

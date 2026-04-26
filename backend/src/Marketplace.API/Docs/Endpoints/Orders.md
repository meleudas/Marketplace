# Orders Controller

## `GET /me/orders`
- **Призначення:** список моїх замовлень (buyer scope).
- **Фільтри:** `statuses[]`, `createdFromUtc`, `createdToUtc`, `search`, `sort`, `page`, `pageSize`.
- **Авторизація:** `[Authorize]`.
- **Кеш:** read-through (list cache keys).

## `GET /me/orders/{orderId}`
- **Призначення:** деталі мого замовлення.
- **Авторизація:** `[Authorize]`.
- **Кеш:** detail cache key `orders:detail:{orderId}`.
- **Response timeline:** включає `statusHistory[]` (old/new status, changedBy, source, correlationId, changedAt).

## `GET /companies/{companyId}/orders`
- **Призначення:** список замовлень компанії (seller scope).
- **Авторизація:** `[Authorize]`, перевірка членства в компанії або Admin.

## `GET /companies/{companyId}/orders/{orderId}`
- **Призначення:** деталі замовлення у межах компанії.
- **Авторизація:** `[Authorize]`, перевірка company access.
- **Response timeline:** включає `statusHistory[]`.

## `GET /admin/orders`
- **Призначення:** адміністраторський список замовлень.
- **Авторизація:** `[Authorize(Roles = \"Admin\")]`.

## `GET /admin/orders/{orderId}`
- **Призначення:** адміністраторські деталі замовлення.
- **Авторизація:** `[Authorize(Roles = \"Admin\")]`.
- **Response timeline:** включає `statusHistory[]`.

## `POST /orders/{orderId}/status`
- **Призначення:** змінити статус (`Processing`, `Shipped`, `Delivered`).
- **Body:** `status`, `trackingNumber?`.
- **Авторизація:** seller компанії або admin.
- **Side effects:** оновлює order status + інвалідує `orders:detail` і list-cache через version bump (`my/company/admin` scope).
- **Event consistency:** публікує `OrderStatusChanged` в outbox для retry-safe internal processing.

## `POST /orders/{orderId}/cancel`
- **Призначення:** скасувати замовлення за політикою.
- **Авторизація:** buyer цього order, seller компанії або admin.
- **Side effects:** встановлює `Cancelled` + інвалідує `orders:detail` і list-cache через version bump (`my/company/admin` scope).
- **Event consistency:** публікує `OrderCancelled` в outbox для idempotent-consumer flow.

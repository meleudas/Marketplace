# Support endpoints

## `POST /support/tickets`

- **Summary (1 рядок):** Створення звернення до підтримки.
- **Призначення:** Користувач відкриває helpdesk ticket з subject/message/priority.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **User**, **Buyer**
- **Бізнес-логіка:**
  1. Перевірити `Support:Enabled`
  2. Rate limit на створення (`SupportAntiAbusePolicy`)
  3. Згенерувати `ticketNumber`, SLA due, audit event `Created`
  4. За `HelpdeskSyncEnabled` — pending external link + outbox `SupportTicketCreated`
- **Side effects (синхронно):** `support_tickets`, `support_ticket_events`, optional `support_external_links`
- **Async / «магія»:** Outbox → `IHelpdeskPort` (LoggingHelpdeskPort у dev)
- **Де на фронті:**
  - Екран: Support ticket form
  - Статус: `backend-only`
- **Приймає:** body `{ subject, message, priority, orderId?, companyId?, categoryId? }`
- **Повертає:** `SupportTicketDto`
- **Помилки:** `503` (disabled), `429`/conflict (rate limit), `401`
- **Примітки:** метрики `support_tickets_total`, `support_ticket_latency_ms`

## `POST /support/tickets/{id}/messages`

- **Summary (1 рядок):** Додати повідомлення до ticket.
- **Призначення:** Клієнт або staff додає public/internal message.
- **Хто може викликати:**
  - JWT: обов'язково
  - Owner ticket або staff (**Support**, **Moderator**, **Admin**)
- **Бізнес-логіка:**
  1. Перевірити доступ (`SupportTicketAccessPolicy`)
  2. Internal messages — лише staff
  3. Append message + audit; outbox `SupportTicketMessageAdded` при sync enabled
- **Side effects (синхронно):** `support_ticket_messages`, оновлення preview/last_message_at
- **Де на фронті:** Support thread — `backend-only`
- **Приймає:** `{ message, isInternal? }`
- **Повертає:** `SupportTicketMessageDto`
- **Помилки:** `403`, `409` (closed ticket)

## `GET /me/support/tickets`

- **Summary (1 рядок):** Inbox звернень поточного користувача.
- **Призначення:** Paginated список tickets користувача.
- **Хто може викликати:** JWT **User/Buyer**
- **Повертає:** `SupportTicketListDto`

## `GET /me/support/tickets/{id}`

- **Summary (1 рядок):** Деталі ticket з повідомленнями.
- **Призначення:** Owner або staff читає ticket + messages (internal лише для staff).
- **Повертає:** `SupportTicketDetailDto`

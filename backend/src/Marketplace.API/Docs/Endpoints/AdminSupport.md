# Admin Support endpoints

## `POST /admin/support/tickets/{id}/assign`

- **Summary (1 рядок):** Призначити ticket модератору/агенту.
- **Призначення:** Staff assign + audit assignment row.
- **Хто може викликати:** **Moderator**, **Admin**
- **Бізнес-логіка:**
  1. `SupportTicket.Assign` → status `Assigned` якщо було `Open`
  2. Append `support_ticket_assignments` + event `Assigned`
  3. Outbox status sync при `HelpdeskSyncEnabled`
- **Приймає:** `{ assigneeUserId, reason }`
- **Повертає:** `SupportTicketDto`

## `POST /admin/support/tickets/{id}/status`

- **Summary (1 рядок):** Зміна статусу ticket (state machine).
- **Призначення:** Resolve/close/pending customer тощо з audit trail.
- **Хто може викликати:** **Moderator**, **Admin**
- **Бізнес-логіка:**
  1. `SupportTicketStatePolicy` + domain `CanTransitionTo`
  2. SLA breach metric якщо прострочено
  3. In-app notification `SupportTicketStatusChanged` для owner
  4. Outbox `SupportTicketStatusChanged`
- **Приймає:** `{ status, reason }`
- **Помилки:** `409` invalid transition

## `POST /admin/support/tickets/{id}/escalate`

- **Summary (1 рядок):** Ескалація ticket (P1/P2 policy hook).
- **Призначення:** Перевести в `Escalated` з audit + helpdesk sync.
- **Хто може викликати:** **Moderator**, **Admin**
- **Приймає:** `{ reason }`

## `POST /integrations/support/helpdesk/webhook`

- **Summary (1 рядок):** Inbound sync від helpdesk-провайдера.
- **Призначення:** Idempotent status merge з dedup (`IInboxDeduplicator`).
- **Хто може викликати:** Anonymous + HMAC `X-Helpdesk-Signature`
- **Gate:** `Support:HelpdeskWebhookEnabled`
- **Приймає:** `{ eventId, externalTicketId, status, updatedAt?, eventSequence? }`
- **Помилки:** `401` (bad signature), `422` (invalid payload), `409` (invalid transition)

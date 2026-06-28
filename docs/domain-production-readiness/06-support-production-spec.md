# 06 - Support Production Spec

## 1. Context and business goals

Support має забезпечити масштабовану підтримку користувачів, але операційно орієнтується на зовнішній helpdesk (Zendesk/Intercom клас).

Ціль - зберегти внутрішній доменний контроль і SLA, передавши agent-операції у зовнішню систему.

## 2. Domain target state

Наявні сутності:

- `Marketplace.Domain/Support/Entities/SupportTicket.cs`
- `Marketplace.Domain/Support/Entities/SupportTicketMessage.cs`

Цільовий стан:

- ticket lifecycle:
  - `Open`, `Assigned`, `PendingCustomer`, `Resolved`, `Closed`, `Escalated`.
- інваріанти:
  - status transitions валідні та audit-овані;
  - escalation policy для P1/P2;
  - зв'язок із `Order`/`Company`/`User` у контексті запиту.

## 3. Application target state

### Команди/запити

- `CreateSupportTicket`
- `AddSupportMessage`
- `UpdateTicketStatus`
- `AssignSupportTicket`
- `EscalateSupportTicket`
- `SyncTicketFromHelpdeskWebhook`
- `GetMySupportTickets`

### Політики

- source of truth:
  - внутрішня система зберігає canonical ticket id і state snapshot;
  - helpdesk id мапиться окремим полем.
- outbound sync і inbound webhook sync ідемпотентні.

## 4. API target state

### Endpoints

- `POST /support/tickets`
- `GET /me/support/tickets`
- `GET /me/support/tickets/{id}`
- `POST /support/tickets/{id}/messages`
- `POST /admin/support/tickets/{id}/assign`
- `POST /admin/support/tickets/{id}/status`
- `POST /integrations/support/helpdesk/webhook`

### Authz

- User: create/read own tickets.
- Support/Admin: assign/status/escalate.
- webhook endpoint: signed requests only.

### Error model

- `404` ticket not found.
- `403` access denied.
- `409` invalid state transition.
- `422` rejected payload from external sync.

## 5. Infrastructure and data model

### Таблиці

- `support_tickets`
- `support_ticket_messages`
- `support_ticket_assignments`
- `support_ticket_events`
- `support_external_links` (ticket_id <-> helpdesk_ticket_id)

### Інтеграція із зовнішнім helpdesk

- Adapter порт `IHelpdeskPort`:
  - create ticket;
  - add comment;
  - sync status;
  - fetch SLA meta.
- Webhook handler:
  - signature validation;
  - dedup by event-id;
  - out-of-order event handling.

### Надійність

- retries + dead-letter на outbound sync.
- periodic reconciliation job (internal vs helpdesk states).

## 6. Security and abuse resistance

- PII handling:
  - маскування персональних даних у logs;
  - retention для support messages.
- anti-abuse:
  - ticket create rate limits;
  - attachment scanning policy.
- повний audit trail для assign/escalate/close.

## 7. Testing strategy and CI gates

### Unit (Suite=Support)

- state machine transitions;
- escalation policy;
- webhook signature validation.

### IntegrationLight

- create ticket -> add message -> close flow.

### IntegrationContainers

- helpdesk adapter contract tests;
- webhook dedup/order consistency;
- reconciliation job behavior.

### E2E

- user opens ticket -> support updates status -> user sees final state.

### CI gate

- `support-gate`:
  - Unit + IntegrationLight (`Suite=Support`)
  - Coverage threshold >= 12%.
- Containers + E2E у `integration-full`.

## 8. Observability and runbook

### Метрики

- `support_tickets_total`
- `support_ticket_errors_total`
- `support_ticket_latency_ms`
- `support_sla_breach_total`
- `support_helpdesk_sync_failures_total`

### Алерти

- sync failures spike;
- SLA breaches зростають;
- webhook lag > threshold.

### Runbook

- helpdesk outage mode;
- manual reconciliation procedure;
- replay failed sync events.

## 9. Release and rollback strategy

- flags:
  - `SupportEnabled`
  - `SupportHelpdeskSyncEnabled`
  - `SupportHelpdeskWebhookEnabled`
- rollout:
  1. internal tickets only;
  2. outbound helpdesk sync;
  3. inbound webhook sync;
  4. reconciliation jobs.
- rollback: disable sync/webhook flags, retain internal ticket operations.

## 10. Definition of Done (100/100)

- [ ] Визначено і реалізовано canonical ticket model + external mapping.
- [ ] Webhook sync і outbound sync ідемпотентні, із dedup та retry/DLQ.
- [ ] SLA breaches вимірюються і алертяться.
- [ ] Є Unit/Integration/E2E покриття і `Suite=Support`.
- [ ] Є інцидентний runbook для деградації helpdesk.

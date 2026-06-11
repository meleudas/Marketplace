# Support context

Bounded context для **formal helpdesk tickets** (canonical model, audit, SLA, external sync).

## Boundaries

| Concern | Support tickets | Chats (`ChatType.Support`) |
|---------|-----------------|----------------------------|
| Model | `SupportTicket` aggregate + messages/assignments/events | `Chat` + `Message` realtime |
| Workflow | State machine, assign, escalate, SLA | Send/read/archive, SignalR |
| External sync | `IHelpdeskPort`, outbox, webhook, reconciliation | Немає у v1 |
| Integration | Окремі API `/support/*`, `/admin/support/*` | `/me/chats/*`, hub |

**v1:** contexts **не змішуються**. Bridge ticket→chat — backlog.

## AuthZ

| Action | Ticket owner | Support staff | Moderator/Admin |
|--------|--------------|---------------|-----------------|
| Create ticket | ✓ | ✓ (as user) | ✓ |
| Read own tickets | ✓ | — | — |
| Read any ticket | — | ✓ | ✓ |
| Add public message | ✓ | ✓ | ✓ |
| Add internal message | — | ✓ | ✓ |
| Assign / status / escalate | — | read-only queue* | ✓ |

\*Role `Support` may read tickets via staff flag on GET; assign/status endpoints require Moderator/Admin.

## Rollout flags

- `Support:Enabled` — internal tickets API
- `Support:HelpdeskSyncEnabled` — outbound outbox + `LoggingHelpdeskPort`
- `Support:HelpdeskWebhookEnabled` — inbound webhook

Rollback order: webhook → sync → `Enabled=false`. Дані зберігаються.

## Persistence

- `support_tickets`, `support_ticket_messages`
- `support_ticket_assignments`, `support_ticket_events` (audit)
- `support_external_links` (provider, external id, sync state, sequence)

## Async jobs

- `outbox-dispatch-pending` — `SupportTicketCreated/MessageAdded/StatusChanged`
- `support-helpdesk-reconcile` (hourly) — links з `sync_state != synced`

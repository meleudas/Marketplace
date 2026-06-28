# Chats bounded context

## Aggregates

- **Chat** — lifecycle `Active` → `Archived` / `Blocked`; типи `Direct`, `OrderRelated`, `Support`
- **Message** — `Sent` → `Read` / `DeletedForPolicy`
- **ChatParticipant** — buyer/seller/support roles, composite PK `(chat_id, user_id)`
- **ChatReadState** — per-user read cursor `(chat_id, user_id)`
- **ChatModerationAction** — append-only audit

## Authz matrix

| Action | Participant | Platform staff (Support/Moderator/Admin) |
|--------|-------------|------------------------------------------|
| Read/write Direct/Order chat | Active participant only | — |
| Read/write Support chat | Buyer participant | Allowed for Support type chats |
| Moderate | — | `POST /admin/chats/{id}/moderate` |

## Integration notes

- Unread badges: `IAppNotificationScheduler` + template `ChatMessageReceived`
- Realtime: SignalR hub `/hubs/chat` (flag `Chats:RealtimeEnabled`)
- **SupportTicket** domain — окремий skeleton; не інтегрований у v1

## Feature flags

- `Chats:Enabled` — HTTP API
- `Chats:ModerationEnabled` — sync content filter + admin moderate
- `Chats:RealtimeEnabled` — SignalR broadcast

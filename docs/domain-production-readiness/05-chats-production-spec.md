# 05 - Chats Production Spec

## 1. Context and business goals

Chats забезпечує комунікацію buyer/seller навколо товарів і замовлень. Ціль - безпечний канал повідомлень із контролем спаму і модерацією.

## 2. Domain target state

Наявні базові сутності:

- `Marketplace.Domain/Chats/Entities/Chat.cs`
- `Marketplace.Domain/Chats/Entities/Message.cs`
- `Marketplace.Domain/Chats/Entities/ChatParticipant.cs`

Цільовий стан:

- chat lifecycle: `Active`, `Archived`, `Blocked`.
- message lifecycle: `Sent`, `Delivered`, `Read`, `DeletedForPolicy`.
- інваріанти:
  - доступ лише для учасників чату;
  - sender має бути активним participant;
  - редагування/видалення обмежено policy rules.

## 3. Application target state

### Команди/запити

- `CreateChat`
- `SendMessage`
- `MarkMessageRead`
- `ListMyChats`
- `GetChatMessages`
- `ArchiveChat`
- `ModerateChatMessage`

### Політики

- anti-spam throttling per sender/chat/window.
- content moderation hooks (sync lightweight + async deep scan).
- read-state оновлюється ідемпотентно.

## 4. API target state

### Endpoints

- `POST /me/chats`
- `GET /me/chats`
- `GET /me/chats/{chatId}/messages`
- `POST /me/chats/{chatId}/messages`
- `POST /me/chats/{chatId}/messages/{messageId}/read`
- `POST /me/chats/{chatId}/archive`
- `POST /admin/chats/{chatId}/moderate`

### Authz

- participants: read/write own chats.
- moderator/admin: moderation actions.

### Error model

- `403` not chat participant.
- `409` archived/blocked chat cannot accept message.
- `429` spam/rate-limit breach.

## 5. Infrastructure and data model

### Таблиці

- `chats`
- `chat_participants`
- `chat_messages`
- `chat_read_states`
- `chat_moderation_actions`

### Технічні вимоги

- pagination indexes for message timeline.
- optional real-time transport (WebSocket/SignalR) + HTTP fallback.
- outbox events у `Notifications` для unread badges.

## 6. Security and abuse resistance

- anti-spam policy:
  - message rate limits;
  - duplicate message suppression.
- moderation:
  - prohibited words/links filters;
  - manual escalation queue.
- retention policy для чутливого контенту.

## 7. Testing strategy and CI gates

### Unit (Suite=Chats)

- participant access rules;
- lifecycle transitions;
- anti-spam decisions.

### IntegrationLight

- send/read/archive flow.

### IntegrationContainers

- high-throughput message burst;
- moderation hooks + queue durability.

### E2E

- buyer-seller chat conversation + read-state + moderation action.

### CI gate

- `chats-gate`:
  - Unit + IntegrationLight (`Suite=Chats`)
  - Coverage threshold >= 12%.

## 8. Observability and runbook

### Метрики

- `chat_messages_total`
- `chat_message_errors_total`
- `chat_message_latency_ms`
- `chat_spam_block_total`
- `chat_unread_backlog`

### Алерти

- send failure spike;
- moderation queue lag.

### Runbook

- spam attack mode (strict throttling profile);
- realtime transport degradation fallback to polling.

## 9. Release and rollback strategy

- flags:
  - `ChatsEnabled`
  - `ChatsRealtimeEnabled`
  - `ChatsModerationEnabled`
- rollout:
  1. HTTP chat basics;
  2. moderation hooks;
  3. real-time channel.
- rollback: disable realtime first, then write endpoints if needed.

## 10. Definition of Done (100/100)

- [ ] Access control строго обмежує чат учасниками.
- [ ] Read-state і moderation працюють у всіх ключових сценаріях.
- [ ] Anti-spam контури мають технічне enforcement.
- [ ] Є Unit/Integration/E2E покриття і `Suite=Chats`.
- [ ] Є операційний runbook для spam/incidents.

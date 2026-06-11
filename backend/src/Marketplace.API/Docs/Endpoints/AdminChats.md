# Admin chats endpoints

## `POST /admin/chats/{chatId}/moderate`

- **Summary (1 рядок):** Модераційна дія над чатом або повідомленням.
- **Призначення:** hide/warn/block chat з audit trail у `chat_moderation_actions`.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Moderator**, **Admin**, **Support**
- **Бізнес-логіка:**
  1. Перевірити `Chats:Enabled` та `Chats:ModerationEnabled`
  2. `Hide` — soft-delete message (`DeletedForPolicy`)
  3. `BlockChat` — `ChatStatus.Blocked`
  4. Append moderation action
- **Body:** `messageId?`, `actionType` (0=Hide, 1=Warn, 2=BlockChat), `reason`
- **Помилки:** `404`, `503` (moderation disabled)
- **Де на фронті:** Admin moderation queue — Статус: `backend-only`

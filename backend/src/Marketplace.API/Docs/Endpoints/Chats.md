# Chats endpoints

## `POST /me/chats`

- **Summary (1 рядок):** Створення або повторне відкриття чату (Direct, OrderRelated, Support).
- **Призначення:** ініціювати buyer/seller/support розмову.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Buyer**, **User**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Перевірити `Chats:Enabled`
  2. Для Direct — `productId`, для OrderRelated — `orderId`, Support — без додаткових полів
  3. Дедуплікація активного чату або повернення існуючого
  4. Додати учасників (buyer + seller або buyer-only для support)
- **Side effects (синхронно):** `chats`, `chat_participants`
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Chat inbox / product «Написати продавцю»
  - API-модуль: —
  - Статус: `backend-only`
- **Body:** `type`, `productId?`, `orderId?`
- **Помилки:** `404`, `409` (disabled/duplicate), `503` (feature flag off).

## `GET /me/chats`

- **Summary (1 рядок):** Inbox чатів поточного користувача.
- **Призначення:** paginated список чатів з unread preview.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Buyer**, **User**
- **Query:** `page`, `size`
- **Повертає:** `items[]`, `total`, `page`, `size`
- **Де на фронті:** Статус: `backend-only`

## `GET /me/chats/{chatId}/messages`

- **Summary (1 рядок):** Timeline повідомлень чату.
- **Призначення:** paginated історія повідомлень (учасник або support staff для Support-чату).
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Buyer**, **User**; **Moderator**, **Admin**, **Support** (для Support type)
- **Query:** `page`, `size`
- **Помилки:** `403` (not participant), `404`

## `POST /me/chats/{chatId}/messages`

- **Summary (1 рядок):** Надіслати повідомлення в чат.
- **Призначення:** send message з anti-spam та moderation hooks.
- **Body:** `text`, `replyToMessageId?`
- **Async / «магія»:** in-app/push через `ChatMessageReceived`; SignalR `MessageReceived` якщо `Chats:RealtimeEnabled`
- **Помилки:** `403`, `409` (archived/blocked), `429` (rate limit), `422` (prohibited content)

## `POST /me/chats/{chatId}/messages/{messageId}/read`

- **Summary (1 рядок):** Ідемпотентне оновлення read-state.
- **Призначення:** позначити повідомлення прочитаним (cursor у `chat_read_states`).
- **Async / «магія»:** SignalR `MessageRead` якщо realtime увімкнено

## `POST /me/chats/{chatId}/archive`

- **Summary (1 рядок):** Архівувати чат для учасника.
- **Помилки:** `403`, `409`

## Realtime (SignalR)

- **Hub:** `/hubs/chat`
- **Auth:** JWT Bearer або `?access_token=` для WebSocket
- **Client methods:** `JoinChat`, `LeaveChat`
- **Server events:** `MessageReceived`, `MessageRead`, `ChatArchived`

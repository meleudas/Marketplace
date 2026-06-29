# Domain Report: Chats

- Статус реалізації: `Implemented`
- Готовність: **78/100**
- Release status: `Conditional`
- Confidence: `Medium`
- Дата аудиту: 2026-06-29

## Evidence

- CI: `chats-gate`
- Container: ChatsModerationPostgresTests
- E2E: SeedCouponReturnChatE2ETests

## Межі домену

- `backend/src/Marketplace.Domain/Chats`
- `backend/src/Marketplace.Application/Chats`
- `ChatsController`, chat repositories

## Що готово

- Buyer/seller messaging, read state, moderation hooks.
- Feature flags `Chats:Enabled`, `Chats:ModerationEnabled`.

## Blockers (P0)

- Немає для MVP (P2 за пріоритетом бізнесу).

## Near-term (P1)

- Dedicated container test для message send/list (окремо від moderation).
- Anti-spam rate limits на messages.

## Checklist

- [x] Full vertical slice
- [x] chats-gate CI
- [x] Moderation container test
- [ ] Message transport container test
- [ ] Real-time delivery strategy documented (polling vs push)

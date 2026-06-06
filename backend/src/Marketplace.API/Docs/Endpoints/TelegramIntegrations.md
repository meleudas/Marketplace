# TelegramIntegrationsController — `/integrations/telegram`

## `POST /integrations/telegram/webhook`

- **Summary (1 рядок):** Webhook Telegram Bot API для прив'язки чату до акаунта.
- **Призначення:** прийом update від Telegram Bot API; при повідомленні виду `/start <linkCode>` — прив'язка чату до акаунта (`LinkTelegramAccountCommand`).
- **Хто може викликати:**
  - JWT: не потрібна (опційна перевірка `X-Telegram-Bot-Api-Secret-Token`)
  - Глобальні ролі: —
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Перевірити секрет заголовка (якщо налаштовано)
  2. Розпарсити `/start <linkCode>` з `message.text`
  3. Виконати `LinkTelegramAccountCommand` для прив'язки чату
- **Side effects (синхронно):** при успішній прив'язці — оновлення даних користувача/2FA (логіка в command handler)
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: —
  - API-модуль: —
  - Статус: `planned` (server-to-server only; URL у BotFather)
- **Приймає:** body `TelegramUpdate` (`message?` з `chat.id`, `text`).
- **Повертає:**
  - **200** порожнє тіло — якщо update прийнято, або формат не `/start ...`, або порожній текст (ігнор).
  - **401** якщо задано `Telegram:WebhookSecret` і заголовок **`X-Telegram-Bot-Api-Secret-Token`** не збігається.
  - **400** без тіла `ProblemDetails` якщо прив'язка не вдалась (`result.IsSuccess == false`).
- **Примітки:** не викликати з публічного фронту магазину — лише URL webhook у BotFather.

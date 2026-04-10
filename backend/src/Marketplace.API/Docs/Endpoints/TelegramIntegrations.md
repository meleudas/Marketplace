# TelegramIntegrationsController — `/integrations/telegram`

## `POST /integrations/telegram/webhook`

- **Призначення:** прийом update від Telegram Bot API; при повідомленні виду `/start <linkCode>` — прив’язка чату до акаунта (`LinkTelegramAccountCommand`).
- **Приймає:** body `TelegramUpdate` (`message?` з `chat.id`, `text`).
- **Повертає:**
  - **200** порожнє тіло — якщо update прийнято, або формат не `/start ...`, або порожній текст (ігнор).
  - **401** якщо задано `Telegram:WebhookSecret` і заголовок **`X-Telegram-Bot-Api-Secret-Token`** не збігається.
  - **400** без тіла `ProblemDetails` якщо прив’язка не вдалась (`result.IsSuccess == false`).
- **Авторизація:** опційна перевірка секрету заголовком (див. вище).
- **Side effects:** при успішній прив’язці — оновлення даних користувача/2FA (логіка в command handler).
- **Примітки:** не викликати з публічного фронту магазину — лише URL webhook у BotFather.

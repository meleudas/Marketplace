# TelegramIntegrationsController

Контролер: `/integrations/telegram/*`

## Авторизація
- Контролер: `AllowAnonymous`
- Якщо задано `Telegram:WebhookSecret`, обов'язковий заголовок:
  - `X-Telegram-Bot-Api-Secret-Token: <secret>`

## Моделі запитів (copy-paste)

### POST `/integrations/telegram/webhook`
```json
{
  "message": {
    "chat": {
      "id": 123456789
    },
    "text": "/start ABCDEF123456"
  }
}
```

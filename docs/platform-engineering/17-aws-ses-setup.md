# 17 — AWS SES setup (email provider)

Основний email-провайдер: **AWS SES** (IAM role). **SendGrid** залишається fallback у коді.

## Передумови

1. Обрати регіон SES (наприклад `eu-central-1`) — той самий, що в `AwsSes:Region`.
2. Верифікувати домен або email у SES Console.
3. Налаштувати DNS: DKIM, SPF, DMARC.
4. Запросити **production access** (вихід з SES Sandbox).

## IAM policy (task / instance role)

Секретів у `.env` не потрібно — SDK використовує default credential chain.

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "MarketplaceSesSend",
      "Effect": "Allow",
      "Action": [
        "ses:SendEmail",
        "ses:SendRawEmail"
      ],
      "Resource": "*"
    },
    {
      "Sid": "MarketplaceSesHealth",
      "Effect": "Allow",
      "Action": [
        "ses:GetAccount"
      ],
      "Resource": "*"
    }
  ]
}
```

Для production обмежте `Resource` до ARN verified identity, якщо політика організації вимагає least privilege.

## Конфігурація API

| Env var | Приклад | Опис |
|---------|---------|------|
| `AWSSES__ENABLED` | `true` | Увімкнути SES |
| `AWSSES__REGION` | `eu-central-1` | Регіон SES |
| `AWSSES__FROMEMAIL` | `noreply@example.com` | Verified sender |
| `AWSSES__FROMNAME` | `Marketplace` | Відображуване ім'я |
| `AWSSES__CONFIGURATIONSETNAME` | *(опційно)* | SES configuration set |

Пріоритет провайдера: **SES → SendGrid → Logging**.

## Rollback

`AWSSES__ENABLED=false` + наявний `SENDGRID__APIKEY` → автоматичний fallback на SendGrid.

## Health

- `GET /health/email` — активний email-провайдер
- `GET /health/sendgrid` — alias (backward compatible)

## Пов'язані документи

- [12-production-secrets-policy.md](12-production-secrets-policy.md)

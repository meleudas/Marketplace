# 12 — Production secrets policy

Політика секретів для Production deploy Marketplace backend.

## Принципи

1. **Жодних dev-placeholder** у `ASPNETCORE_ENVIRONMENT=Production`.
2. Секрети **не комітяться** в git; лише [backend/.env.example](../../backend/.env.example) як шаблон.
3. **Fail-fast** при старті API: [ProductionConfigurationValidator.cs](../../backend/src/Marketplace.Infrastructure/Configuration/ProductionConfigurationValidator.cs).
4. Pre-deploy перевірка: `backend/scripts/ci/validate-production-env.ps1` або `validate-production-env.sh`.

## Таблиця секретів

| Secret | Env var (double underscore) | Обов'язковий у prod | Rotation |
|--------|----------------------------|---------------------|----------|
| JWT signing key | `JWT__SECRETKEY` | Так (≥32 байт) | Кожні 90 днів або при компрометації |
| LiqPay public/private | `LIQPAY__PUBLICKEY`, `LIQPAY__PRIVATEKEY` | Так | За політикою LiqPay |
| Google OAuth | `GOOGLEAUTH__CLIENTID`, `GOOGLEAUTH__CLIENTSECRET` | Якщо OAuth увімкнено | При ротації OAuth client |
| AWS SES | `AWSSES__ENABLED`, `AWSSES__REGION`, `AWSSES__FROMEMAIL` | Якщо `APPNOTIFICATIONS__EMAILENABLED=true` і SES — основний провайдер | IAM role на ECS/EC2 (без secret key) |
| SendGrid | `SENDGRID__APIKEY` | Fallback email, якщо SES вимкнено | За політикою SendGrid |
| Web Push VAPID | `WEBPUSH__PUBLICKEY`, `WEBPUSH__PRIVATEKEY` | Якщо `WEBPUSH__ENABLED=true` | Рідко; invalidate subscriptions |
| MinIO | `STORAGE__ACCESSKEY`, `STORAGE__SECRETKEY` | Якщо `STORAGE__ENABLED=true` і `STORAGE__PROVIDER=Minio` | Кожні 90 днів |
| AWS S3 | `STORAGE__PROVIDER=AwsS3`, `STORAGE__BUCKET`, `STORAGE__REGION` | Якщо `STORAGE__ENABLED=true` і Provider=AwsS3; Access/Secret **не потрібні** при IAM Role на ECS/EC2/EKS | IAM role (без static keys) |
| Nova Poshta | `NOVAPOSHTA__APIKEY` | Якщо `SHIPPING__NOVAPOSHTAENABLED=true` | За політикою NP |
| Postgres | `POSTGRES_PASSWORD` | Так (compose) | Scheduled + on incident |
| ClickHouse | `CLICKHOUSE_PASSWORD` | Якщо ClickHouse enabled | Scheduled |

## Заборонені значення (Production)

| Поле | Заборонено |
|------|------------|
| `Jwt:SecretKey` | `ChangeThis`, `DevSecret`, `AtLeast32`, `YourSecret`, `ReplaceMe` |
| `LiqPay:*` | `sandbox_test`, `sandbox`, `test` |
| `Storage:*` (Provider=Minio) | `minioadmin` / `minioadmin` pair |
| `Storage:Provider` | значення поза `Minio` / `AwsS3` |
| `GoogleAuth` | Напівконфіг (лише ClientId або лише ClientSecret) |

## Pre-deploy checklist

- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] `./backend/scripts/ci/validate-production-env.ps1 -EnvFile backend/.env` → PASS
- [ ] `dotnet run --project backend/src/Marketplace.API -- --validate-config-only` → PASS
- [ ] LiqPay keys — production (не sandbox)
- [ ] JWT — унікальний, ≥32 символів
- [ ] Postgres password — не `postgres`
- [ ] Backup Postgres виконано перед deploy

## Валідація в коді

```csharp
// Program.cs — після Build()
ProductionConfigurationValidator.Validate(app.Configuration, app.Environment);
```

CLI без запуску сервера:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
dotnet run --project backend/src/Marketplace.API -- --validate-config-only
```

## Пов'язані документи

- [17-aws-ses-setup.md](17-aws-ses-setup.md)
- [13-production-deploy-runbook.md](13-production-deploy-runbook.md)
- [10-staging-production-rollout.md](10-staging-production-rollout.md)
- [reports/production-readiness/security-readiness.md](../../reports/production-readiness/security-readiness.md)

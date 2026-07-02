# Звіт про критичні проблеми Backend проєкту Marketplace

**Дата аналізу:** 2026-06-30  
**Статус запуску:** ✅ Backend успішно запущено в dev режимі  
**Аналізовані компоненти:** Backend API (.NET 10), Infrastructure, Security, Configuration

---

## 🔴 КРИТИЧНІ ПРОБЛЕМИ (P0 - Потребують негайного виправлення)

### 1. **Вразливості в залежностях OpenTelemetry** 
**Severity:** HIGH  
**Статус:** Активні вразливості  

**Деталі:**
- **OpenTelemetry.Api 1.14.0** - 1 moderate severity vulnerability (GHSA-g94r-2vxg-569j)
- **OpenTelemetry.Exporter.OpenTelemetryProtocol 1.14.0** - 3 moderate severity vulnerabilities:
  - GHSA-4625-4j76-fww9
  - GHSA-mr8r-92fq-pj8p
  - GHSA-q834-8qmm-v933

**Локація:**
```
backend/src/Marketplace.API/Marketplace.API.csproj (lines 17-25)
```

**Вплив:** 
- Потенційні вразливості в телеметрії та моніторингу
- Можливість витоку метрик або DoS атак через OTLP exporter

**Рекомендації:**
1. Оновити всі OpenTelemetry пакети до останніх стабільних версій (1.15+)
2. Перевірити наявність патчів на GitHub Advisory Database
3. Якщо патчів немає - розглянути тимчасове вимкнення OpenTelemetry в production (`OTEL_ENABLED=false`)

**Команда для оновлення:**
```bash
cd backend/src/Marketplace.API
dotnet add package OpenTelemetry --version 1.15.0
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol --version 1.15.0
# Повторити для всіх OpenTelemetry.* пакетів
```

---

### 2. **Слабкий JWT Secret за замовчуванням**
**Severity:** CRITICAL  
**Статус:** Конфігураційна вразливість  

**Деталі:**
- В `.env` файлі використовується dev secret: `ChangeThis_DevSecretKey_AtLeast32CharsLong!!`
- Цей же секрет присутній в `appsettings.json` як fallback
- Хоча є `ProductionConfigurationValidator` для production, dev середовище незахищене

**Локація:**
```
backend/.env (line 63)
backend/src/Marketplace.API/appsettings.json (line 162)
```

**Вплив:**
- Можливість підробки JWT токенів в dev середовищі
- Якщо dev база містить реальні дані - повна компрометація
- Ризик випадкового deploy з dev секретом

**Рекомендації:**
1. ✅ Валідатор вже існує для production - це добре
2. ❌ Змінити dev secret на унікальний навіть для локальної розробки
3. Додати попередження в README про обов'язкову зміну секрета
4. Розглянути генерацію випадкового секрета при створенні .env

**Приклад генерації:**
```bash
openssl rand -base64 32
```

---

### 3. **Порожні credentials для інтеграцій**
**Severity:** HIGH  
**Статус:** Конфігурація не готова до production  

**Деталі:**
В `.env` файлі всі critical secrets порожні:
- `GOOGLEAUTH__CLIENTID=`
- `GOOGLEAUTH__CLIENTSECRET=`
- `SENDGRID__APIKEY=`
- `TELEGRAM__BOTTOKEN=`
- `LIQPAY__PUBLICKEY=`
- `LIQPAY__PRIVATEKEY=`
- `WEBPUSH__PUBLICKEY=`
- `WEBPUSH__PRIVATEKEY=`

**Вплив:**
- Google OAuth не працює - користувачі не можуть авторизуватись через Google
- Email notification не працює - не надсилаються підтвердження, паролі, тощо
- Telegram 2FA не працює
- LiqPay платежі не працюють
- Web Push notifications не працюють

**Рекомендації:**
1. **Dev середовище:** Створити test credentials для кожного сервісу
2. **Staging/Production:** Використовувати secrets management (GitHub Secrets, Azure Key Vault, AWS Secrets Manager)
3. Додати checklist в README з переліком обов'язкових credentials
4. Налаштувати health checks для перевірки доступності інтеграцій

---

### 4. **DataProtection Keys не персистуються**
**Severity:** MEDIUM  
**Статус:** Warning в логах  

**Деталі:**
```
warn: Microsoft.AspNetCore.DataProtection.Repositories.FileSystemXmlRepository[60]
      Storing keys in a directory '/root/.aspnet/DataProtection-Keys' that may not be persisted 
      outside of the container. Protected data will be unavailable when container is destroyed.
```

**Вплив:**
- Всі зашифровані дані (cookies, CSRF tokens, тощо) стануть недоступними після restart контейнера
- Користувачі будуть вилогінені після кожного deploy
- Refresh tokens стануть невалідними

**Рекомендації:**
1. Налаштувати DataProtection з Redis або database persistence:
```csharp
services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>()
    .SetApplicationName("Marketplace");
```
2. Або змонтувати volume для `/root/.aspnet/DataProtection-Keys`

---

## ⚠️ ВАЖЛИВІ ПРОБЛЕМИ (P1 - Виправити до production)

### 5. **HTTPS Redirect не налаштований**
**Severity:** MEDIUM  

**Деталі:**
```
warn: Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware[3]
      Failed to determine the https port for redirect.
```

**Вплив:**
- Cookies можуть бути перехоплені через незахищене з'єднання
- Refresh tokens передаються в незашифрованому вигляді

**Рекомендації:**
1. Налаштувати reverse proxy (nginx/Traefik) з TLS termination для production
2. Додати `UseHttpsRedirection()` з правильним портом в Program.cs
3. Встановити `Secure` flag для cookies в production

---

### 6. **Відсутність rate limiting для критичних endpoint-ів**
**Severity:** MEDIUM  

**Деталі:**
- Хоча є `RedisRateLimitCounterStore`, не всі критичні endpoints захищені
- Endpoints типу `/auth/login`, `/auth/register`, `/account/forgot-password` вразливі до brute-force

**Рекомендації:**
1. Додати rate limiting для auth endpoints (5 спроб / 15 хв)
2. Додати rate limiting для password reset (3 спроби / год)
3. Реалізувати progressive delays після невдалих спроб

---

### 7. **Null Reference Warnings в коді**
**Severity:** LOW  

**Деталі:**
```
/src/src/Marketplace.Application/Payments/Commands/HandleLiqPayWebhook/HandleLiqPayWebhookCommandHandler.cs(149,33): 
warning CS8604: Possible null reference argument for parameter 'transactionId'
```

**Рекомендації:**
1. Додати null checks або null-forgiving operator
2. Увімкнути `TreatWarningsAsErrors` для production builds

---

### 8. **Duplicate using directive**
**Severity:** LOW  

**Деталі:**
```
/src/src/Marketplace.Infrastructure/Jobs/AppNotificationJobs.cs(9,7): 
warning CS0105: The using directive for 'Marketplace.Application.Common.Observability' appeared previously
```

**Рекомендації:**
- Видалити дублікат для чистоти коду

---

## ✅ ПОЗИТИВНІ МОМЕНТИ

### Що реалізовано правильно:

1. ✅ **ProductionConfigurationValidator** - відмінна ідея fail-fast валідації
   - Перевіряє JWT secret на dev placeholders
   - Валідує minioadmin credentials
   - Перевіряє SendGrid API key формат
   - Блокує sandbox LiqPay keys в production

2. ✅ **Архітектура безпеки:**
   - Strong password policy (12 chars, uppercase, digit, special)
   - Lockout policy (5 attempts, 15 min)
   - Email confirmation required
   - Refresh token rotation

3. ✅ **Authorization:**
   - Role-based access control (Admin, Moderator, Support)
   - Proper `[Authorize]` attributes на controllers
   - Domain access services для products, orders, companies

4. ✅ **Database security:**
   - EF Core parameterized queries (не знайдено SQL injection вразливостей)
   - Soft delete для users
   - Audit trails (order history, report actions)

5. ✅ **.env в .gitignore** - secrets не комітяться в репозиторій

6. ✅ **Comprehensive migrations:** 65+ міграцій, добре структуровані

7. ✅ **Health checks:** PostgreSQL, Redis, Elasticsearch, ClickHouse, MinIO

---

## 📊 СТАТИСТИКА

- **API Endpoints:** 164
- **Controllers:** 30+
- **Database Tables:** 76
- **Migrations:** 65+
- **Docker Services:** 6 (API, PostgreSQL, Redis, Elasticsearch, MinIO, ClickHouse)
- **Security Score (за власним звітом):** 91/100

---

## 🚀 ПЛАН ДІЙ

### Негайно (Сьогодні):
1. Згенерувати новий JWT secret для dev середовища
2. Створити test credentials для Google OAuth, SendGrid (використати Gmail SMTP)
3. Налаштувати DataProtection persistence

### Цього тижня:
4. Оновити OpenTelemetry пакети або вимкнути в production
5. Налаштувати HTTPS redirect для production
6. Додати rate limiting для auth endpoints
7. Виправити compiler warnings

### До production deploy:
8. Створити production credentials для всіх інтеграцій
9. Налаштувати secrets management (Azure Key Vault / AWS Secrets Manager)
10. Провести penetration testing
11. Налаштувати TLS/SSL certificates
12. Налаштувати monitoring та alerting для security events

---

## 📝 ВИСНОВОК

**Загальна оцінка:** 7/10

**Проєкт має міцну архітектурну основу з правильними security patterns**, але потребує:
- Оновлення вразливих залежностей
- Налаштування production credentials
- Вирішення конфігураційних проблем (DataProtection, HTTPS)

**Для dev середовища:** Проєкт готовий до роботи після додавання test credentials  
**Для production:** Потрібно 2-3 дні на виправлення критичних issues

**Рекомендація:** Не deployити в production до вирішення P0 issues (пункти 1-4).

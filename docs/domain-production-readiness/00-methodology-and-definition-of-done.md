# 00 - Methodology and Definition of Done

## Мета

Уніфікувати вимоги до gap-доменів так, щоб їх можна було довести до production-ready рівня без неоднозначностей.

Базова шкала та ваги узгоджені з [reports/production-readiness/scoring-methodology.md](../../reports/production-readiness/scoring-methodology.md).

## Definition of Done для домену (100/100)

Домен вважається готовим до production, якщо виконані всі пункти нижче.

### 1) Domain model

- Є чіткі aggregate boundaries та інваріанти.
- Всі критичні правила домену виражені у Value Objects/Enums/Policies.
- Відсутні "сирі" бізнес-правила в контролерах.

### 2) Application layer

- Описані команди/запити для всіх основних сценаріїв.
- Є транзакційні межі для write-flow.
- Є ідемпотентність для endpoint-ів із ризиком повторів.
- Error model повертає стабільні `ProblemDetails` для клієнтів.

### 3) API layer

- Для всіх маршрутів є контракти request/response.
- Є authz matrix (хто може читати/писати/модерувати).
- Негативні кейси (403/404/409/422) явно визначені.

### 4) Infrastructure + Data

- Є таблиці, індекси, constraints, міграції.
- Критичні race-condition закриті індексами/locking-політиками.
- Описані зовнішні інтеграції, retries, timeout, fallback, DLQ.

### 5) Security + Abuse control

- Є rate limiting та anti-abuse правила для публічних/чутливих endpoint-ів.
- PII policy визначає зберігання, маскування, ретеншн.
- Audit trail присутній для адміністративних/фінансово значущих дій.

### 6) Testing + CI

- Unit, IntegrationLight, IntegrationContainers, E2E покривають ключові сценарії.
- Є `Suite=<DomainName>` для доменного gate.
- Coverage threshold не нижче 12% для domain-focused gate.
- Security/contract/performance смоук-сценарії підключені до CI.

### 7) Observability + Operations

- Є RED/USE метрики, трейсинг і структуровані логи.
- Є алерти на error-rate, latency, деградацію інтеграцій.
- Є runbook для інцидентів, rollback і production smoke checks.

## Стандартна структура domain-spec

Кожен файл `NN-<domain>-production-spec.md` має містити:

1. Context and business goals
2. Domain target state
3. Application target state
4. API target state
5. Infrastructure and data model
6. Security and abuse resistance
7. Testing strategy and CI gates
8. Observability and runbook
9. Release plan and rollback strategy
10. Definition of Done checklist

## CI/Gate policy (уніфіковано)

- Обов'язково:
  - `dotnet test backend/tests/Marketplace.Tests.Unit --filter "Suite=<DomainName>"`
  - `dotnet test backend/tests/Marketplace.Tests.Integration --filter "Suite=<DomainName>"`
- Для інтеграцій із зовнішніми сервісами:
  - `Marketplace.Tests.Integration.Containers` з `Layer=IntegrationContainers`
  - `Marketplace.Tests.E2E` з `Layer=E2E`

## Вимоги до якості документації

- Кожен пункт повинен бути перевірним результатом.
- Не використовувати розмиті формулювання без owner/умов завершення.
- Дозволені TODO лише з чітким trigger-умовами, а не "колись потім".

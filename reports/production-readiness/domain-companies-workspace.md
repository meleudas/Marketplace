# Domain Report: Companies & Workspace

- Статус: `Implemented`
- Оцінка готовності: **100/100**

## Межі домену

- `backend/src/Marketplace.Domain/Companies`
- `backend/src/Marketplace.Application/Companies`
- `backend/src/Marketplace.API/Controllers/AdminCatalogController.cs`
- `backend/src/Marketplace.API/Controllers/CompanyMembersController.cs`
- `backend/src/Marketplace.Infrastructure/Persistence/Repositories/Company*`

## Що вже готово

- Модель компаній, членства, ролей, commission rates.
- Адмінські сценарії approve/revoke та керування членами.
- Базові тести для доменної логіки та частини API.
- Закриті lifecycle edge-cases: commission policy лише для approved компаній; last-owner guard; non-admin owner escalation guard.
- Репозиторій членства узгоджений із soft-delete (permission checks/listing/owner existence працюють тільки по active membership).
- Додані SQLite integration/e2e сценарії для company lifecycle, membership flow і tenant isolation.
- Додані API + contract тести для `AdminCatalogController` і `CompanyMembersController`.
- Додані metrics/logging для approve/revoke/member-role-change операцій + оновлений observability runbook.
- Додано окремий CI gate `companies-workspace-gate` з coverage threshold.

## Blockers

- Blockers закриті.

## Near-term

- Підтримувати coverage threshold і розширювати сценарії при додаванні нових ролей/прав.
- За потреби винести membership audit trail у окремий immutable журнал подій.

## Optional

- Автоматизувати компанійний compliance-checklist.
- Додати richer events для нотифікацій компанійних змін.

## Мінімальний checklist

- [x] AuthZ матриця по ролях протестована.
- [x] Команди membership мають негативні тести.
- [x] Є журналювання критичних змін membership/approval.
- [x] Ролі Admin/Owner/Manager ізольовані між компаніями.

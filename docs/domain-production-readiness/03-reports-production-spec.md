# 03 - Reports Production Spec

## 1. Context and business goals

Reports потрібен для user-generated скарг на продукти, відгуки, компанії та комунікацію. Ціль - керований moderation pipeline з SLA та ескалаціями.

## 2. Domain target state

Наявна сутність:

- `Marketplace.Domain/Reports/Entities/Report.cs`

Цільовий стан:

- aggregate `ReportCase` з lifecycle:
  - `New`, `InReview`, `Actioned`, `Rejected`, `Escalated`, `Closed`.
- `ReportTargetType` охоплює мінімум: product, review, company, chat-message.
- інваріанти:
  - reporter не може модерати власний report;
  - case action потребує moderator/admin role;
  - зміна статусу фіксує actor + reason.

## 3. Application target state

### Команди/запити

- `CreateReport`
- `GetMyReports`
- `GetModerationQueue`
- `AssignReportCase`
- `ResolveReportCase`
- `EscalateReportCase`
- `BulkModerationAction`

### Політики

- SLA policy:
  - P1 report -> first review <= 2h;
  - P2 -> <= 24h.
- дедуп подібних репортів в короткому вікні.
- необоротність ключових статусів після `Closed`.

## 4. API target state

### Endpoints

- `POST /reports`
- `GET /me/reports`
- `GET /admin/reports/queue`
- `POST /admin/reports/{id}/assign`
- `POST /admin/reports/{id}/resolve`
- `POST /admin/reports/{id}/escalate`
- `POST /admin/reports/bulk-actions`

### Authz

- Buyer/User: create/read own reports.
- Moderator/Admin: moderation queue/actions.
- Support: read-only visibility для синхронізації з support flows.

### Error model

- `404` target resource not found.
- `409` state conflict (already closed/actioned).
- `422` invalid action for current status.

## 5. Infrastructure and data model

### Таблиці

- `reports`
- `report_actions` (audit log)
- `report_assignments`
- `report_escalations`

### Індекси

- queue index: `(status, priority, created_at)`.
- dedup index on `(reporter_user_id, target_type, target_id, reason, created_bucket)`.

### Інтеграції

- notification hooks у `Notifications` (new report, assigned, SLA breach).
- link на `Reviews`/`Products`/`Companies` для action side-effects.

## 6. Security and abuse resistance

- Anti-spam:
  - rate limit `POST /reports`;
  - cooldown між однаковими скаргами.
- Evidence policy:
  - media attachments з malware scan/allowlist MIME.
- Audit:
  - усі модераційні рішення незмінно журналюються.

## 7. Testing strategy and CI gates

### Unit (Suite=Reports)

- lifecycle transitions;
- SLA breach classification;
- dedup policy.

### IntegrationLight

- create report, queue visibility, assign/resolve transitions.

### IntegrationContainers

- concurrent moderation actions consistency;
- outbox notifications на SLA breach.

### E2E

- user creates report -> moderator resolves -> user sees final status.

### CI gate

- `reports-gate`:
  - Unit + IntegrationLight (`Suite=Reports`)
  - Coverage threshold >= 12%
- full suite для `integration-full`.

## 8. Observability and runbook

### Метрики

- `report_operations_total`
- `report_errors_total`
- `report_sla_breach_total`
- `report_queue_backlog`
- `report_resolution_latency_ms`

### Алерти

- backlog > threshold;
- SLA breach spike;
- moderation error rate spike.

### Runbook

- backlog drain procedure;
- temporary auto-triage rules for incident mode.

## 9. Release and rollback strategy

- feature flags:
  - `ReportsPublicCreateEnabled`
  - `ReportsModerationEnabled`
- rollout:
  1. queue read + internal tooling;
  2. public create;
  3. SLA alerts + escalations.
- rollback: disable public create, keep moderation pipeline alive for open cases.

## 10. Definition of Done (100/100)

- [ ] Є повний report lifecycle з moderation queue та audit trail.
- [ ] SLA breach детектиться автоматично та алертиться.
- [ ] Є інтеграція з notifications для operational прозорості.
- [ ] Є Unit/Integration/E2E покриття та `Suite=Reports` gate.
- [ ] Публічний create захищений від spam/abuse.

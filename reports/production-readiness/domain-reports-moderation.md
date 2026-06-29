# Domain Report: Reports (Content Moderation)

- Статус реалізації: `Implemented`
- Готовність: **80/100**
- Release status: `Conditional`
- Confidence: `High`
- Дата аудиту: 2026-06-29

## Evidence

- CI: `reports-gate`
- Container: ReportsWorkflowPostgresTests (create → review → resolve → close)
- Integration Light: IntegrationReportsSqliteTests

## Межі домену

- `backend/src/Marketplace.Domain/Reports`
- `backend/src/Marketplace.Application/Reports`
- Reports API, moderation queue queries

## Що готово

- Report lifecycle з state machine (New → InReview → Actioned → Closed).
- Dedup, rate limit, audit trail.
- Reporter cannot moderate own report.

## Blockers (P0)

- Немає.

## Near-term (P1)

- E2E public create report flow.
- SLA notifications (ReportSlaPolicy) у prod monitoring.

## Checklist

- [x] Create + moderation handlers
- [x] reports-gate CI
- [x] Container workflow test
- [ ] E2E moderation queue

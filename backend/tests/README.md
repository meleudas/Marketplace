# Backend test pyramid

## Projects

| Project | Layer trait | Purpose | CI |
|---------|-------------|---------|-----|
| `Marketplace.Tests.Common` | — | Fakes, `SqliteDbFixture`, shared builders | — |
| `Marketplace.Tests.Unit` | `Unit` | Domain, handlers, API wiring, contracts, security | Every PR |
| `Marketplace.Tests.Integration` | `IntegrationLight` | SQLite handler + repository flows | Every PR |
| `Marketplace.Tests.Integration.Containers` | `IntegrationContainers` | Postgres, Redis, Elasticsearch, MinIO, ClickHouse via Testcontainers | PR label `integration-full`, **push `main`**, release tag `v*` |
| `Marketplace.Tests.E2E` | `E2E` | HTTP via `WebApplicationFactory` | PR label `integration-full`, **push `main`**, release tag `v*` |

Domain folders mirror `Docs/Endpoints` (`IdentityAccess`, `Cart`, `Orders`, `Payments`, `Platform`, …).

## Local commands

```powershell
# Fast (every PR)
dotnet test backend/tests/Marketplace.Tests.Unit
dotnet test backend/tests/Marketplace.Tests.Integration

# Full (Docker Desktop required, ~4 GB RAM recommended for ES 512m + ClickHouse)
# Mandatory on main merge (integration-full-main) and release tags (backend-release workflow)
dotnet test backend/tests/Marketplace.Tests.Integration.Containers --filter "Layer=IntegrationContainers"
dotnet test backend/tests/Marketplace.Tests.E2E --filter "Layer=E2E"

# Pre-release deploy smoke (postgres + API + /health/ready)
./backend/scripts/ci/deploy-smoke.sh

# Seeded P4 scenarios (requires Testcontainers)
dotnet test backend/tests/Marketplace.Tests.E2E --filter "Layer=E2E&Suite=Seed"

# Catalog smoke — all discovered controller routes (no HTTP 500)
dotnet test backend/tests/Marketplace.Tests.E2E --filter "Suite=ApiCatalogSmoke"
```

Seed data is loaded from `backend/scripts/seed-test-data.sql` via `TestSeedDataLoader` in `Marketplace.Tests.Common` (users: `buyer@marketplace.test`, `seller@marketplace.test`, `admin@marketplace.test`, password `Admin123!`).

Filter by domain suite: `--filter "Suite=CartCheckout"`.

## Coverage gates (CI)

| Gate | Threshold (line %) | Notes |
|------|-------------------|-------|
| Global `unit-coverage-gate` | 10 | Phase A (target 25) |
| P0 scoped (Cart, Payments, Orders, Identity) | 14 | `Include=` domain assemblies |
| Інші domain gates | 12 | |

Baseline: [coverage-baseline-2026-06-29.md](../../reports/production-readiness/evidence/coverage-baseline-2026-06-29.md).

## PR rules

1. Changing a public endpoint → update `ENDPOINT_COVERAGE_MATRIX.md` and add at least one test in the appropriate layer.
2. Slow suites (`Integration.Containers`, `E2E`) run in CI only when the PR has label **`integration-full`**.
3. Keep `Suite=*` traits on test classes; add `Layer=*` on container/E2E tests (unit/integration layers use `AssemblyInfo.cs`).

## Coverage matrix

See [ENDPOINT_COVERAGE_MATRIX.md](./ENDPOINT_COVERAGE_MATRIX.md).

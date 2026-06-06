# Backend test pyramid

## Projects

| Project | Layer trait | Purpose | CI |
|---------|-------------|---------|-----|
| `Marketplace.Tests.Common` | — | Fakes, `SqliteDbFixture`, shared builders | — |
| `Marketplace.Tests.Unit` | `Unit` | Domain, handlers, API wiring, contracts, security | Every PR |
| `Marketplace.Tests.Integration` | `IntegrationLight` | SQLite handler + repository flows | Every PR |
| `Marketplace.Tests.Integration.Containers` | `IntegrationContainers` | Postgres, Redis, Elasticsearch, MinIO via Testcontainers | PR label `integration-full` |
| `Marketplace.Tests.E2E` | `E2E` | HTTP via `WebApplicationFactory` | PR label `integration-full` |

Domain folders mirror `Docs/Endpoints` (`IdentityAccess`, `Cart`, `Orders`, `Payments`, `Platform`, …).

## Local commands

```powershell
# Fast (every PR)
dotnet test backend/tests/Marketplace.Tests.Unit
dotnet test backend/tests/Marketplace.Tests.Integration

# Full (Docker Desktop required)
dotnet test backend/tests/Marketplace.Tests.Integration.Containers --filter "Layer=IntegrationContainers"
dotnet test backend/tests/Marketplace.Tests.E2E --filter "Layer=E2E"
```

Filter by domain suite: `--filter "Suite=CartCheckout"`.

## PR rules

1. Changing a public endpoint → update `ENDPOINT_COVERAGE_MATRIX.md` and add at least one test in the appropriate layer.
2. Slow suites (`Integration.Containers`, `E2E`) run in CI only when the PR has label **`integration-full`**.
3. Keep `Suite=*` traits on test classes; add `Layer=*` on container/E2E tests (unit/integration layers use `AssemblyInfo.cs`).

## Coverage matrix

See [ENDPOINT_COVERAGE_MATRIX.md](./ENDPOINT_COVERAGE_MATRIX.md).

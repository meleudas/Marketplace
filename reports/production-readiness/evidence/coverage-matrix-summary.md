# Evidence summary — 2026-06-29

Артефакти зібрані під час production-readiness аудиту.

## Build

| Перевірка | Результат | Артефакт |
|-----------|-----------|----------|
| `dotnet build backend/Marketplace.slnx -c Release` | **PASS** (0 errors, 56 warnings) | [build-release.log](./build-release.log) |

## Tests

| Suite | Filter | Passed | Failed | Duration | Артефакт |
|-------|--------|--------|--------|----------|----------|
| Unit | all | 443 | 0 | ~9s | [test-unit.log](./test-unit.log) |
| Integration Light | all | 50 | 0 | ~12s | [test-integration-light.log](./test-integration-light.log) |
| Integration Containers | `Layer=IntegrationContainers` | 32 | 0 | ~45s | [test-containers.log](./test-containers.log) |
| E2E | `Layer=E2E` | 35 | 0 | ~11s | [test-e2e.log](./test-e2e.log) |
| **Разом** | | **560** | **0** | | |

## Vulnerabilities

| Перевірка | Результат | Артефакт |
|-----------|-----------|----------|
| `dotnet list package --vulnerable --include-transitive` | Moderate: OpenTelemetry 1.14.0; High: SQLitePCLRaw 2.1.11 (test transitive) | [vulnerable-packages.txt](./vulnerable-packages.txt) |
| `check_vulnerabilities.py` | Не виконано локально (Python недоступний); CI policy блокує лише High/Critical у prod graph | — |

## Endpoint coverage matrix (P0 + P4)

Джерело: [backend/tests/ENDPOINT_COVERAGE_MATRIX.md](../../backend/tests/ENDPOINT_COVERAGE_MATRIX.md)

| Колонка | Рядків з покриттям | Усього рядків (оцінка) | % |
|---------|-------------------|------------------------|---|
| U (Unit) | 44 | 57 | 77% |
| L (Integration Light) | 22 | 57 | 39% |
| C (Containers) | 35 | 57 | 61% |
| E (E2E) | 28 | 57 | 49% |

Примітки: P4 секція дублює деякі маршрути; catalog smoke покриває всі HTTP routes на рівні «не 500».

## CI jobs (mandatory on PR)

Джерело: [.github/workflows/backend-ci.yml](../../.github/workflows/backend-ci.yml)

- Mandatory: api-regression, contract-compat, security-regression, performance-baseline, 15× domain gates, unit-coverage-gate (8%), architecture-gate, observability-config-validate
- Conditional: `integration-full` (label), `sonar-analysis` (vars.SONAR_HOST_URL)

## Health checks

Джерело: `HealthCheckRegistrationExtensions.cs` — postgres (live+ready), redis, elasticsearch, storage, outbox queue, clickhouse (if enabled), recommendation_model (if enabled).

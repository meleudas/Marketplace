# 08 — CI/CD інтеграція

## 1. Scope & non-goals

**Scope:** нові GitHub Actions jobs для architecture, Sonar, observability config validate.

**Non-goals:** Заміна domain coverage gates.

## 2. As-is

[`.github/workflows/backend-ci.yml`](../../.github/workflows/backend-ci.yml) — api-regression, contract, security, domain gates з Coverlet.

## 3. To-be

Додаткові jobs (не блокують один одного, крім optional `needs`):

| Job | Filter / command |
|-----|------------------|
| `architecture-gate` | `--filter "Suite=Architecture"` |
| `observability-config-validate` | `otelcol validate`, `promtool check rules` |
| `sonar-analysis` | dotnet-sonarscanner begin/end + tests |

## 4. Покрокова інтеграція

### architecture-gate

```yaml
architecture-gate:
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 10.0.x
    - run: dotnet restore backend/Marketplace.slnx
    - run: dotnet build backend/Marketplace.slnx -c Release --no-restore
    - run: dotnet test backend/tests/Marketplace.Tests/Marketplace.Tests.csproj -c Release --no-build --filter "Suite=Architecture" --logger "trx;LogFileName=architecture-tests.trx" --results-directory backend/test-results
```

### observability-config-validate

```yaml
observability-config-validate:
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - name: Validate OTEL Collector config
      run: |
        docker run --rm -v "${{ github.workspace }}/observability:/cfg" \
          otel/opentelemetry-collector-contrib:0.96.0 \
          validate --config=/cfg/otel-collector-config.yaml
    - name: Install promtool
      run: |
        curl -sL https://github.com/prometheus/prometheus/releases/download/v2.51.2/prometheus-2.51.2.linux-amd64.tar.gz | tar xz
        ./prometheus-2.51.2.linux-amd64/promtool check rules observability/prometheus-rules.yml
```

### sonar-analysis

```yaml
sonar-analysis:
  runs-on: ubuntu-latest
  if: ${{ vars.SONAR_HOST_URL != '' || secrets.SONAR_TOKEN != '' }}
  steps:
    - uses: actions/checkout@v4
      with:
        fetch-depth: 0
    - uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 10.0.x
    - name: Install scanner
      run: dotnet tool install --global dotnet-sonarscanner
    - name: Begin Sonar
      env:
        SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
        SONAR_HOST_URL: ${{ vars.SONAR_HOST_URL }}
      run: |
        dotnet sonarscanner begin \
          /k:"marketplace-backend" \
          /d:sonar.host.url="$SONAR_HOST_URL" \
          /d:sonar.token="$SONAR_TOKEN" \
          /d:sonar.cs.opencover.reportsPaths="backend/test-results/**/coverage.cobertura.xml"
    - run: dotnet restore backend/Marketplace.slnx
    - run: dotnet build backend/Marketplace.slnx -c Release
    - run: |
        dotnet test backend/tests/Marketplace.Tests/Marketplace.Tests.csproj -c Release --no-build \
          /p:CollectCoverage=true \
          /p:CoverletOutput=backend/test-results/sonar-coverage/ \
          /p:CoverletOutputFormat=cobertura
    - name: End Sonar
      env:
        SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
      run: dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"
```

**Secrets:** `SONAR_TOKEN`. **Variables:** `SONAR_HOST_URL` (напр. `http://sonar.internal:9000`).

## 5. Конфігурація

Coverlet merge для Sonar: один прогін full test suite або reportPaths glob.

## 6. Безпека

Secrets не логувати в stdout scanner.

## 7. CI/CD

Domain gates залишаються паралельними; Sonar не змінює coverlet thresholds.

## 8. Верифікація

Green workflow на PR до `main`.

## 9. Rollback

Закоментувати jobs або `if: false` тимчасово.

## 10. Definition of Done

- [x] Три нові jobs у backend-ci.yml.
- [x] Architecture tests green (7 rules + `ObservabilityRegistrationTests`).
- [x] Collector/rules validate green.
- [x] Sonar documented для self-hosted URL + quality-gate wait.

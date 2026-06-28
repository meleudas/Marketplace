# 02 — NetArchTest (архітектурні правила шарів)

> **Примітка:** для .NET використовується **[NetArchTest](https://github.com/BenMorris/NetArchTest)** — аналог JVM ArchUnit. Пакет `ArchUnit` у Java не застосовується до цього solution.

## 1. Scope & non-goals

**Scope:** автоматична перевірка залежностей між шарами DDD/Hexagonal.

**Non-goals:** стиль коду (Sonar), cyclomatic complexity, runtime behavior.

## 2. As-is

- Правила описані в [`backend/ARCHITECTURE_DECISION_MATRIX.md`](../../backend/ARCHITECTURE_DECISION_MATRIX.md).
- Автотестів архітектури немає.

## 3. To-be

Тести в `backend/tests/Marketplace.Tests/Architecture/ArchitectureTests.cs`, trait `[Trait("Suite", "Architecture")]`.

## 4. Покрокова інтеграція

### 4.1 NuGet

```xml
<PackageReference Include="NetArchTest.Rules" Version="1.3.2" />
```

### 4.2 Правила

| ID | Твердження | NetArchTest ідея |
|----|------------|------------------|
| ARCH-01 | Domain не залежить від Application/Infrastructure/API | `Types.InAssembly(Domain).ShouldNot().HaveDependencyOnAny(...)` |
| ARCH-02 | Application не залежить від Infrastructure/API | `ShouldNot().HaveDependencyOn("Marketplace.Infrastructure")` |
| ARCH-03 | API Controllers без Persistence/Jobs/Identity/Caching/External (Observability metrics дозволено) | `HaveDependencyOnAny` заборонені namespaces |
| ARCH-04 | Application handlers без EF/Npgsql/Hangfire | `ShouldNot().HaveDependencyOnAny("Microsoft.EntityFrameworkCore", ...)` |
| ARCH-05 | Domain entities без EF attributes | `ShouldNot().HaveDependencyOn("System.ComponentModel.DataAnnotations.Schema")` — optional, якщо немає таких атрибутів |
| ARCH-06 | CommandHandlers у `*.Commands.*`, QueryHandlers у `*.Queries.*` | Naming / namespace rules |
| ARCH-07 | Port interfaces в Application, implementations в Infrastructure | Types ending with `Repository` in Domain only as interfaces |

### 4.3 Приклад тесту

```csharp
[Fact]
[Trait("Suite", "Architecture")]
public void Domain_should_not_reference_outer_layers()
{
    var result = Types.InAssembly(DomainAssembly)
        .ShouldNot()
        .HaveDependencyOnAny(
            "Marketplace.Application",
            "Marketplace.Infrastructure",
            "Marketplace.API")
        .GetResult();

    Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypes));
}
```

### 4.4 Винятки

- `IgnoreTypes(typeof(...))` для legacy types (тимчасово).
- Політика: нові порушення = fail CI; існуючі — fix або explicit ignore з TODO.

### 4.5 CI

```yaml
architecture-gate:
  run: dotnet test ... --filter "Suite=Architecture"
```

## 5. Конфігурація

Без окремого config файлу; правила — код у `ArchitectureTests.cs`.

## 6. Безпека

N/A (compile-time only).

## 7. CI/CD

Job `architecture-gate` — блокує merge при fail.

## 8. Верифікація

```powershell
dotnet test backend/tests/Marketplace.Tests/Marketplace.Tests.csproj --filter "Suite=Architecture"
```

Очікування: усі тести Passed.

## 9. Rollback / troubleshooting

| Симптом | Дія |
|---------|-----|
| False positive на MediatR | Перевірити, чи handler не в Application з reflection |
| Failing type list великий | По одному ARCH rule; тимчасовий `IgnoreTypes` |

## 10. Definition of Done

- [x] `NetArchTest.Rules` у test project.
- [x] ARCH-01..04 + ARCH-06 enforced у CI (`architecture-gate`).
- [ ] ARCH-05..07 documented (optional enforce).

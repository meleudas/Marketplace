# Архітектура шарів і життєвий цикл HTTP-запиту

Документ описує **призначення типів файлів** у кожному проєкті бекенду, **зв’язок із бібліотеками** та **ланцюжок обробки запиту** на прикладі поточної структури (`Marketplace.API` → `Application` → `Domain` ← `Infrastructure`).

---

## Залежності між проєктами

```text
Marketplace.API
    → Marketplace.Application
    → Marketplace.Domain
    → Marketplace.Infrastructure
          → Marketplace.Application
          → Marketplace.Domain
```

**Правило:** `Domain` не залежить ні від чого. `Application` залежить лише від `Domain`. `Infrastructure` реалізує контракти `Application` і працює з БД/зовнішніми сервісами. `API` збирає все в одне застосування.

---

## 1. `Marketplace.Domain`

Чистий домен: бізнес-модель і правила без фреймворків (окрім базового .NET).

| Тип / папка | Призначення | Бібліотеки |
|-------------|-------------|------------|
| **`Entities/`**, **`AggregateRoot`**, **`AuditableSoftDeleteAggregateRoot`** | Сутності та агрегати, інваріанти, `Reconstitute` для відновлення з БД | Тільки .NET |
| **`ValueObjects/`** (у т.ч. `Money`, `JsonBlob`, `*Id` у `EntityLongIds`, `EntityGuidIds`) | Незмінні значення, типобезпечні ідентифікатори | Тільки .NET |
| **`Enums/`** | Перелічувані стани, узгоджені з `smallint`/enum у БД | Тільки .NET |
| **`Repositories/`** (`IUserRepository`, …) | **Інтерфейси** доступу до сховища; реалізації — у Infrastructure | Тільки .NET |
| **`Events/`** | Доменні події (якщо використовуються для side effects) | Тільки .NET |
| **`Common/Exceptions/`** | Доменні винятки (`DomainException`, …) | Тільки .NET |
| **`Shared/Kernel/Result.cs`** | Універсальний результат операції `Success` / `Failure` без прив’язки до HTTP | Тільки .NET |

**Пакети в `.csproj`:** зазвичай відсутні (лише SDK).

---

## 2. `Marketplace.Application`

Use cases: що саме робить система у відповідь на наміри користувача. **Без** прямого HTTP і **без** конкретної БД.

| Тип / папка | Призначення | Бібліотеки |
|-------------|-------------|------------|
| **`Commands/*/*Command.cs`** | Запис `record` з даними наміру (CQRS «команда») | **MediatR** (`IRequest<Result<…>>` або `IRequest<Result>`) |
| **`Commands/*/*CommandHandler.cs`** | Оркестрація: виклик **портів**, репозиторіїв, мапінг у DTO, повернення `Result` | **MediatR** (`IRequestHandler<,>`), **Domain** |
| **`Commands/*/*CommandValidator.cs`** | Правила валідації вхідних полів команди | **FluentValidation** (`AbstractValidator<T>`) |
| **`Queries/*/*Query.cs`** | Читання без зміни стану (аналогічно команді, але семантика «запит») | **MediatR** |
| **`Queries/*/*QueryHandler.cs`** | Завантаження даних, мапінг у DTO | **MediatR**, **Domain** |
| **`Ports/`** (`IAuthenticationPort`, `IEmailPort`, `ITelegramPort`, `ITokenPort`, `ISmsPort`, …) | Контракти на зовнішні можливості (Identity, пошта, SMS, JWT тощо). Реалізації — у Infrastructure | Тільки .NET (+ доменні типи в сигнатурах) |
| **`DTOs/`** | Об’єкти відповіді/переносу дані для API (не доменні сутності) | Тільки .NET |
| **`Mappings/`** | Перетворення між доменом, портами та DTO (`AuthMapper`, …) | Тільки .NET |
| **`Common/Behaviors/`** (`ValidationBehavior`) | Пайплайн MediatR: до хендлера можуть виконуватись cross-cutting кроки (у нас — FluentValidation) | **MediatR** (`IPipelineBehavior`), **FluentValidation** |
| **`Common/Exceptions/`** | Винятки рівня застосування (якщо є) | Тільки .NET |
| **`Users/Services/`** (`IUserReadService`, …) | Явні сервіси застосування, коли потік не через одну команду/запит (наприклад, читання з контролера) | **Microsoft.Extensions.DependencyInjection** (реєстрація в API/Infrastructure) |
| **`DependencyInjection.cs`** | `AddApplication()`: MediatR, behaviors, `AddValidatorsFromAssembly` | **MediatR**, **FluentValidation.DependencyInjectionExtensions** |

**Пакети (`Marketplace.Application.csproj`):** `MediatR`, `FluentValidation`, `FluentValidation.DependencyInjectionExtensions`, `Serilog` (якщо логують з Application).

---

## 3. `Marketplace.Infrastructure`

Технічні деталі: EF Core, Identity, Redis, SendGrid, Telegram, JWT.

| Тип / папка | Призначення | Бібліотеки |
|-------------|-------------|------------|
| **`Persistence/`** (`ApplicationDbContext`, конфігурації сутностей, міграції) | Доступ до PostgreSQL | **EF Core**, **Npgsql.EntityFrameworkCore.PostgreSQL** |
| **`Persistence/Repositories/`** | Реалізації `IUserRepository` та ін. | **EF Core** |
| **`Persistence/Interceptors/`** | Глобальні правила (soft delete, audit timestamps) | **EF Core** |
| **`Identity/`** | `IdentityAuthService` → `IAuthenticationPort`, користувачі, токени | **ASP.NET Core Identity**, **EF Core Identity** |
| **`Identity/Services/TokenService.cs`** | Реалізація `ITokenPort` (JWT) | **System.IdentityModel.Tokens.Jwt**, **Microsoft.AspNetCore.Authentication.JwtBearer** (налаштування в DI) |
| **`External/Email/`**, **`Telegram/`**, **`Sms/`**, **`OAuth/`** | Реалізації `IEmailPort`, `ITelegramPort`, `ISmsPort`, Google OAuth | **SendGrid**, **HttpClient**, пакети Google OAuth (через API-проєкт) |
| **`Caching/`** | Redis / in-memory кеш | **Microsoft.Extensions.Caching.StackExchangeRedis**, `IDistributedCache` |
| **`DependencyInjection.cs`** | Реєстрація DbContext, Identity, JWT, портів, репозиторіїв | **Microsoft.Extensions.* ** |

**Пакети:** `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.Extensions.Caching.StackExchangeRedis`, `SendGrid`, `System.IdentityModel.Tokens.Jwt`, тощо.

---

## 4. `Marketplace.API`

Вхідна точка HTTP: маршрути, авторизація, серіалізація, OpenAPI.

| Тип / папка | Призначення | Бібліотеки |
|-------------|-------------|------------|
| **`Controllers/`** | Приймають body/query, викликають `ISender.Send(...)` або сервіси, виставляють cookies, повертають `IActionResult` | **ASP.NET Core MVC** |
| **`Middleware/`** (`ErrorHandlerMiddleware`, `JwtCookieMiddleware`, …) | Конвеєр запиту: помилки, витяг токена з cookie, … | **ASP.NET Core** |
| **`Filters/`** (`ValidateModelAttribute`) | Додаткова перевірка `ModelState` (data annotations на DTO запиту) | **ASP.NET Core MVC** |
| **`Extensions/`** (`ResultExtensions`, `ServiceCollectionExtensions`, …) | Мапінг `Result` → HTTP статуси / `ProblemDetails`, збірка DI | **ASP.NET Core** |
| **`Options/`** | Strongly-typed конфіг (`CookieAuthOptions`, …) | **Microsoft.Extensions.Options** |
| **`Program.cs`** | Хост, middleware pipeline, Swagger/Scalar/OpenAPI, мапінг endpoints | **ASP.NET Core**, **Swashbuckle**, **Scalar**, **Microsoft.AspNetCore.OpenApi** |

**Пакети:** `Microsoft.AspNetCore.OpenApi`, `Swashbuckle.AspNetCore`, `Scalar.AspNetCore`, `Microsoft.OpenApi`, OAuth Google, тощо.

---

## Життєвий цикл типового запиту (команда через MediatR)

Нижче — **повний ланцюжок** для сценарію на кшталт `POST /auth/login` з `LoginCommand`.

```mermaid
sequenceDiagram
    participant Client
    participant Kestrel as ASP.NET Core (Kestrel)
    participant MW as Middleware (JWT cookie, ErrorHandler, …)
    participant Ctrl as Controller
    participant Filter as ValidateModelAttribute
    participant MediatR as MediatR ISender
    participant VB as ValidationBehavior
    participant FV as FluentValidation
    participant H as LoginCommandHandler
    participant Port as IAuthenticationPort
    participant Inf as IdentityAuthService (Infrastructure)
    participant Id as ASP.NET Identity + EF

    Client->>Kestrel: HTTP POST + JSON body
    Kestrel->>MW: pipeline
    MW->>Ctrl: AuthController.Login
    Ctrl->>Filter: OnActionExecuting (ModelState)
    alt ModelState invalid
        Filter-->>Client: 400 ValidationProblemDetails
    end
    Ctrl->>MediatR: Send(LoginCommand)
    MediatR->>VB: pipeline
    VB->>FV: ValidateAsync(LoginCommand)
    alt validation failures
        VB-->>Ctrl: Result.Failure (без виклику Handler)
    end
    VB->>H: next()
    H->>Port: LoginAsync(...)
    Port->>Inf: реалізація
    Inf->>Id: UserManager / SignInManager / токени
    Id-->>Inf: результат
    Inf-->>H: Result (доменний/портовий)
    H-->>MediatR: Result&lt;AuthTokensDto&gt;
    MediatR-->>Ctrl: Result
    Ctrl->>Ctrl: ToActionResult(), cookies (якщо успіх)
    Ctrl-->>Client: 200 JSON або ProblemDetails (401/403/400)
```

### Коротко покроково

1. **Kestrel** приймає HTTP-запит.
2. **Middleware** (порядок залежить від `UseMarketplaceMiddleware()`): може підставити refresh token, обробити неперехоплені винятки в `ErrorHandlerMiddleware`.
3. **Routing** веде до **Controller**.
4. **`ValidateModelAttribute`** перевіряє **data annotations** на моделі запиту (`LoginRequest` тощо), якщо вона прив’язана до action.
5. Контролер викликає **`ISender.Send(command)`** (**MediatR**).
6. **`ValidationBehavior`** запускає **FluentValidation** для цієї команди; при помилках повертає **`Result.Failure`** і хендлер не виконується.
7. **`CommandHandler`** викликає **порти** та/або **репозиторії** (інтерфейси з Domain/Application, реалізації з Infrastructure).
8. Handler повертає **`Result<T>`** або **`Result`** з **Domain.Shared.Kernel**.
9. Контролер перетворює результат через **`ResultExtensions.ToActionResult`** на **200 OK** + DTO або **ProblemDetails** з відповідним статусом.

### Альтернативні шляхи

- **Запити (`Queries`)** — той самий MediatR-пайплайн; зазвичай без зміни стану, часто через `IUserRepository` / сервіси читання.
- **Прямий виклик сервісу** з контролера (`IUserReadService`) — обходить MediatR; підходить для простих read-сценаріїв, які ви навмисно не оформили як Query.
- **Необроблений виняток** у handler — перехоплюється **ErrorHandlerMiddleware** і перетворюється на відповідь з тілом помилки (залежно від типу винятку).

---

## Де шукати що (шпаргалка)

| Питання | Де дивитись |
|---------|-------------|
| Які поля приймає use case? | `*Command.cs` / `*Query.cs` |
| Бізнес-логіка сценарію | `*Handler.cs` |
| Правила валідації полів | `*Validator.cs` + FluentValidation |
| Контракт на email/SMS/auth | `Application/.../Ports/` |
| Реалізація email/БД/Identity | `Infrastructure/...` |
| Модель даних і правила домену | `Domain/...` |
| HTTP-шлях і код відповіді | `API/Controllers/`, `ResultExtensions` |

---

*Документ можна оновлювати при появі нових шаблонів (наприклад, окремі `DomainEventHandler`, інтеграційні події, outbox).*

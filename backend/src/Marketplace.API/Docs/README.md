# Документація Marketplace.API

Канонічний опис HTTP API та бізнес-логіки для цього проєкту.

## Навігація

| Розділ | Зміст |
|--------|--------|
| [Endpoints — довідник по маршрутах](Endpoints/README.md) | Усі endpoint-и: що приймають, що повертають, авторизація, side effects, помилки |
| [ControllerModels — приклади JSON body](ControllerModels/README.md) | Швидкі зразки тіл запитів для Postman/curl |
| [DDD — контексти та логіка](DDD/README.md) | Bounded contexts, зв’язки сутностей, потоки, кеш, ролі (огляд) |
| [Матриця доступів за ролями](DDD/RoleAccessMatrix.md) | Хто що може в API |
| [Тестові сутності та сценарії](DDD/TestEntitiesAndScenarios.md) | Які дані/ролі потрібні для перевірки |
| [Бізнес-потоки](DDD/BusinessFlows.md) | Словами: що відбувається на бекенді крок за кроком |

## Загальні правила

- **Формат:** `application/json`, властивості в **camelCase**.
- **Авторизація:** `Authorization: Bearer <accessToken>` для `[Authorize]`.
- **Refresh:** дублюється в тілі (де передбачено) і в HTTP-only cookie `refresh_token` (ім’я з `CookieAuthOptions`).
- **Помилки:** зазвичай `ProblemDetails` (`title`, `detail`, `status`); коди залежать від `detail` (див. `ResultExtensions`).

## Довідник полів DTO (великі таблиці)

Детальні таблиці полів JSON залишено в кореневому [README.md](../README.md) проєкту API (розділ «Довідник полів»). Нові endpoint-и описані в [Endpoints/](Endpoints/README.md) з посиланням на типи відповідей.

## OpenAPI / Swagger

- UI: `/swagger`
- OpenAPI JSON: `/openapi/v1.json`

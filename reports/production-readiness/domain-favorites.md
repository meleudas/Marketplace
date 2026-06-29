# Domain Report: Favorites

- Статус реалізації: `Implemented`
- Готовність: **84/100**
- Release status: `Conditional`
- Confidence: `High`
- Дата аудиту: 2026-06-29

## Evidence

- CI: `favorites-gate`
- Container: FavoritesPostgresTests

## Межі домену

- `backend/src/Marketplace.Domain/Favorites`
- `backend/src/Marketplace.Application/Favorites`
- `backend/src/Marketplace.API/Controllers/FavoritesController.cs`
- `backend/src/Marketplace.Infrastructure/Persistence/Repositories/FavoriteRepository.cs`

## Що вже готово

- Базовий add/remove/list workflow реалізований.
- Простий домен без складних транзакційних залежностей.
- Закрито data-integrity blocker для re-add після soft-delete (active-only unique індекс + reactivate flow).
- Додано race-safe поведінку add для конкурентних запитів.
- Додано повний тестовий контур: application/validator, API regression, SQLite integration/e2e, contract, security, performance.
- Додано observability: favorites metrics, логування та runbook alerts.
- Додано окремий CI gate `favorites-gate` з coverage threshold.

## Blockers

- Blockers закриті.

## Near-term

- Підтримувати `Suite=Favorites` і розширювати role/edge coverage при зміні фічі.
- Розвивати персоналізацію favorites (логику `notifications`/аналітику) без регресій поточного контракту.

## Optional

- Додати персоналізовані ліміти/квоти та телеметрію використання.

## Мінімальний checklist

- [x] API favorites має позитивні й негативні тести.
- [x] Доступ строго user-scoped.
- [x] Додано базовий контракт DTO.

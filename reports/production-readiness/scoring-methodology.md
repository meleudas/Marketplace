# Методика оцінки продакшен-готовності

## Шкала

- Кожен домен оцінюється у діапазоні `0..100`.
- `90-100`: production-ready з мінімальними ризиками.
- `75-89`: можна релізити з контрольованим ризиком.
- `60-74`: рання production-ready фаза, потрібні обов'язкові доробки.
- `40-59`: частково готовий, реліз ризикований.
- `0-39`: концепт або незавершена реалізація.

## Ваги критеріїв

| Критерій | Вага |
|---|---:|
| Architecture & Code Health | 20% |
| Security & Access Control | 20% |
| Data & Migrations | 15% |
| API Robustness & Error Handling | 15% |
| Testing & CI Coverage | 20% |
| Operational Readiness | 10% |

Сума ваг: `100%`.

## Як рахується оцінка

`Score = sum(criterion_score * weight)`

де `criterion_score` для кожного критерію у діапазоні `0..100`.

## Статуси доменів

- `Implemented`: є повний вертикальний зріз (Domain + Application + API + Infrastructure).
- `Partial`: є частина шарів, але немає production-критичних елементів.
- `Conceptual`: є тільки доменна модель/документація без робочого потоку.

## Блокери та типи робіт

- **Blockers**: критичні речі, без яких реліз у прод не рекомендується.
- **Near-term**: обов'язкові доробки найближчого спринту після/перед релізом.
- **Optional**: покращення, що знижують довгостроковий ризик.

## Джерела даних оцінки

- Код доменів: `backend/src/Marketplace.Domain`, `backend/src/Marketplace.Application`.
- Контролери й контракти: `backend/src/Marketplace.API/Controllers`, `backend/src/Marketplace.API/Docs`.
- Інфра і міграції: `backend/src/Marketplace.Infrastructure`, `.../Persistence/Migrations`.
- CI та тести: `.github/workflows/backend-ci.yml`, `backend/tests/Marketplace.Tests`.
- Конфіг і запуск: `backend/.env.example`, `backend/src/Marketplace.API/appsettings.json`, `docker-compose.yml`.

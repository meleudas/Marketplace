# Admin analytics endpoints

## `GET /admin/analytics/kpi/summary`

- **Summary (1 рядок):** Зведені KPI за діапазоном дат.
- **Призначення:** summary KPI по діапазону дат.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Перевірити роль Admin
  2. Агрегувати KPI за query-параметрами діапазону дат
  3. Повернути summary DTO
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / analytics dashboard (planned)
  - API-модуль: —
  - Статус: `partial`

## `GET /admin/analytics/kpi/funnel`

- **Summary (1 рядок):** Воронка конверсії view → click → add-to-cart.
- **Призначення:** conversion funnel (`view -> click -> add-to-cart`).
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Перевірити роль Admin
  2. Агрегувати події воронки за період
  3. Повернути funnel DTO
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / analytics funnel (planned)
  - API-модуль: —
  - Статус: `partial`

## `GET /admin/analytics/kpi/top-queries`

- **Summary (1 рядок):** Топ пошукових запитів за період.
- **Призначення:** top search queries за період.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Перевірити роль Admin
  2. Агрегувати search-history за період
  3. Повернути топ запитів
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / top queries (planned)
  - API-модуль: —
  - Статус: `partial`

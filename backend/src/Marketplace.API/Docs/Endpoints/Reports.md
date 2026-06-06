# Reports endpoints

## `POST /reports`

- **Summary (1 рядок):** Створення скарги/репорту від користувача.
- **Призначення:** створити новий report від користувача.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Buyer**, **User** (автентифікований)
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Валідувати body (`targetType`, `targetId`, `reason`, `description`)
  2. Перевірити dedup/rate limit
  3. Створити report-case у черзі модерації
- **Side effects (синхронно):** новий запис report
- **Async / «магія»:** можлива нотифікація модераторам (outbox)
- **Де на фронті:**
  - Екран: Report dialog
  - API-модуль: —
  - Статус: `planned`
- **Body:** `targetType`, `targetId`, `reason`, `description`, `priority`, `images[]`.
- **Помилки:** `409` (dedup/rate limit), `422` (validation), `503` (feature flag off).

## `GET /me/reports`

- **Summary (1 рядок):** Список репортів поточного користувача.
- **Призначення:** список report-ів поточного користувача.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Buyer**, **User**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Визначити `userId` з JWT
  2. Завантажити reports користувача
  3. Повернути список
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: My reports (planned)
  - API-модуль: —
  - Статус: `planned`

## `GET /admin/reports/queue`

- **Summary (1 рядок):** Черга модерації репортів.
- **Призначення:** moderation queue для нових/in-review/escalated cases.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Moderator**, **Admin**, **Support** (read-only)
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Перевірити роль Moderator/Admin/Support
  2. Завантажити cases у статусах new/in-review/escalated
  3. Повернути чергу
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / reports queue (planned)
  - API-модуль: —
  - Статус: `partial`

## `POST /admin/reports/{id}/assign`

- **Summary (1 рядок):** Призначення модератора на report-case.
- **Призначення:** призначити модератора для report-case.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Moderator**, **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти report-case за `id`
  2. Призначити модератора
  3. Оновити статус case
- **Side effects (синхронно):** оновлення assignee та статусу
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / assign report (planned)
  - API-модуль: —
  - Статус: `partial`

## `POST /admin/reports/{id}/resolve`

- **Summary (1 рядок):** Вирішення report-case з moderation action.
- **Призначення:** виконати moderation action і, опційно, одразу закрити case.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Moderator**, **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти report-case
  2. Застосувати moderation action
  3. Закрити case (опційно)
- **Side effects (синхронно):** оновлення статусу case та target entity
- **Async / «магія»:** можлива нотифікація автору report
- **Де на фронті:**
  - Екран: AdminShell / resolve report (planned)
  - API-модуль: —
  - Статус: `partial`

## `POST /admin/reports/{id}/escalate`

- **Summary (1 рядок):** Ескалація report-case.
- **Призначення:** ескалувати case.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Moderator**, **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти report-case
  2. Встановити escalated статус
  3. Записати причину ескалації
- **Side effects (синхронно):** оновлення статусу case
- **Async / «магія»:** можлива нотифікація старшим модераторам
- **Де на фронті:**
  - Екран: AdminShell / escalate report (planned)
  - API-модуль: —
  - Статус: `partial`

## `POST /admin/reports/bulk-actions`

- **Summary (1 рядок):** Пакетні moderation actions для репортів.
- **Призначення:** пакетні moderation actions (`resolve` або `escalate`).
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Moderator**, **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Валідувати список case id та action type
  2. Застосувати action до кожного case
  3. Повернути результат bulk-операції
- **Side effects (синхронно):** масове оновлення cases
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / bulk reports (planned)
  - API-модуль: —
  - Статус: `partial`

# Reports endpoints

## `POST /reports`
- **Призначення:** створити новий report від користувача.
- **Авторизація:** `User`, `Buyer`.
- **Body:** `targetType`, `targetId`, `reason`, `description`, `priority`, `images[]`.
- **Помилки:** `409` (dedup/rate limit), `422` (validation), `503` (feature flag off).

## `GET /me/reports`
- **Призначення:** список report-ів поточного користувача.
- **Авторизація:** `User`, `Buyer`.

## `GET /admin/reports/queue`
- **Призначення:** moderation queue для нових/in-review/escalated cases.
- **Авторизація:** `Moderator`, `Admin`, `Support` (read-only).

## `POST /admin/reports/{id}/assign`
- **Призначення:** призначити модератора для report-case.
- **Авторизація:** `Moderator`, `Admin`.

## `POST /admin/reports/{id}/resolve`
- **Призначення:** виконати moderation action і, опційно, одразу закрити case.
- **Авторизація:** `Moderator`, `Admin`.

## `POST /admin/reports/{id}/escalate`
- **Призначення:** ескалувати case.
- **Авторизація:** `Moderator`, `Admin`.

## `POST /admin/reports/bulk-actions`
- **Призначення:** пакетні moderation actions (`resolve` або `escalate`).
- **Авторизація:** `Moderator`, `Admin`.

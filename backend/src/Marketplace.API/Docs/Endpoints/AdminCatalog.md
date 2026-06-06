# AdminCatalogController — `/admin`

Усі маршрути: **`[Authorize(Roles = "Admin")]`** + Bearer.

## Компанії

### `GET /admin/companies`

- **Summary (1 рядок):** Повний список компаній для адмін-панелі.
- **Призначення:** повний список компаній (у т.ч. не схвалені).
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Перевірити роль Admin
  2. Завантажити всі компанії з репозиторію
  3. Повернути масив `CompanyDto`
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / companies (planned)
  - API-модуль: —
  - Статус: `partial`
- **Повертає:** масив `CompanyDto`.

### `GET /admin/companies/pending`

- **Summary (1 рядок):** Список компаній, що очікують схвалення.
- **Призначення:** компанії з `isApproved = false`.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Перевірити роль Admin
  2. Відфільтрувати компанії з `isApproved = false`
  3. Повернути масив `CompanyDto`
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / pending companies (planned)
  - API-модуль: —
  - Статус: `partial`
- **Повертає:** масив `CompanyDto`.

### `GET /admin/companies/{id}`

- **Summary (1 рядок):** Деталі компанії за id (будь-який стан).
- **Призначення:** одна компанія за id (будь-який стан схвалення та soft-delete).
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти компанію за `id`
  2. Повернути `CompanyDto` або 404
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / company detail (planned)
  - API-модуль: —
  - Статус: `partial`
- **Приймає:** path `id` (guid).
- **Повертає:** **200** `CompanyDto`.
- **Помилки:** **404** `Company not found`.

### `POST /admin/companies`

- **Summary (1 рядок):** Створення нової компанії адміном.
- **Призначення:** створити компанію від імені адміністратора.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Валідувати `CreateCompanyRequest`
  2. Створити запис компанії в БД
  3. Повернути `CompanyDto` з новим `id`
- **Side effects (синхронно):** нова компанія в БД
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / create company (planned)
  - API-модуль: —
  - Статус: `partial`
- **Приймає:** body `CreateCompanyRequest` (див. кореневий README, таблиці полів).
- **Повертає:** **200** створений `CompanyDto` (`id` — новий `Guid`).

### `PUT /admin/companies/{id}`

- **Summary (1 рядок):** Оновлення реквізитів компанії.
- **Призначення:** оновити дані компанії за id.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти компанію за `id`
  2. Застосувати `UpdateCompanyRequest`
  3. Повернути оновлений `CompanyDto`
- **Side effects (синхронно):** оновлення запису компанії
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / edit company (planned)
  - API-модуль: —
  - Статус: `partial`
- **Приймає:** path `id` (guid); body `UpdateCompanyRequest`.
- **Повертає:** оновлений `CompanyDto`.

### `POST /admin/companies/{id}/approve`

- **Summary (1 рядок):** Схвалення компанії для публічного каталогу.
- **Призначення:** схвалити компанію від імені поточного адміна.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти компанію та встановити `isApproved = true`
  2. Записати `approvedAt`, `approvedByUserId` з JWT
  3. Авто-створити `company_contracts` та початкову `company_commission_rates` (якщо ще немає)
- **Side effects (синхронно):** `isApproved`, `approvedAt`, `approvedByUserId`, контракт і комісія
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / approve company (planned)
  - API-модуль: —
  - Статус: `partial`
- **Приймає:** path `id`; з JWT береться `adminUserId` для аудиту.
- **Повертає:** **200** порожнє тіло при успіху.
- **Observability labels:** `admin_approve_company`.

### `POST /admin/companies/{id}/revoke-approval`

- **Summary (1 рядок):** Скасування схвалення компанії.
- **Призначення:** зняти апрув — компанія зникає з публічного каталогу.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти схвалену компанію
  2. Скасувати апрув (`isApproved = false`)
  3. Інвалідувати кеш публічного каталогу
- **Side effects (синхронно):** скасування апруву
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / revoke approval (planned)
  - API-модуль: —
  - Статус: `partial`
- **Повертає:** **200** порожнє тіло.
- **Observability labels:** `admin_revoke_company`.

### `POST /admin/companies/{id}/commission-rates`

- **Summary (1 рядок):** Нова версія комісійної ставки компанії.
- **Призначення:** створити нову версію відсоткової комісії компанії.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Перевірити, що компанія **approved**
  2. Закрити попередню активну ставку (`effectiveTo`)
  3. Додати новий запис `company_commission_rates`
- **Side effects (синхронно):** нова версія ставки комісії
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / commission rates (planned)
  - API-модуль: —
  - Статус: `partial`
- **Приймає:** path `id` + body:
  - `commissionPercent` (`0 < x <= 100`)
  - `effectiveFrom` (UTC дата старту)
  - `reason` (optional)
- **Повертає:** **200** порожнє тіло.
- **Обмеження:** ставка дозволена лише для **approved** компаній.
- **Observability labels:** `admin_set_company_commission`.

### `DELETE /admin/companies/{id}`

- **Summary (1 рядок):** М'яке видалення компанії.
- **Призначення:** soft-delete компанії за id.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти компанію за `id`
  2. Виконати soft-delete
- **Side effects (синхронно):** soft-delete компанії
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / delete company (planned)
  - API-модуль: —
  - Статус: `partial`
- **Повертає:** **200** порожнє тіло.

## Observability (компанійні операції)

- Метрики: `company_operations_total`, `company_errors_total`, `company_latency_ms`.
- Ключові operations labels: `admin_get_companies`, `admin_get_pending_companies`, `admin_get_company_by_id`, `admin_create_company`, `admin_update_company`, `admin_approve_company`, `admin_revoke_company`, `admin_set_company_commission`, `admin_delete_company`.

## Категорії

### `GET /admin/categories`

- **Summary (1 рядок):** Усі категорії каталогу (включно з неактивними).
- **Призначення:** повний список категорій для адмін-панелі.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Завантажити всі категорії
  2. Повернути масив `CategoryDto`
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / categories (planned)
  - API-модуль: —
  - Статус: `partial`
- **Повертає:** масив `CategoryDto` (усі).

### `GET /admin/categories/active`

- **Summary (1 рядок):** Лише активні категорії.
- **Призначення:** список активних категорій для адмін-перегляду.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Відфільтрувати активні категорії
  2. Повернути масив `CategoryDto`
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / categories (planned)
  - API-модуль: —
  - Статус: `partial`
- **Повертає:** масив `CategoryDto` лише активні.

### `GET /admin/categories/{id}`

- **Summary (1 рядок):** Деталі категорії за id.
- **Призначення:** одна категорія за id (будь-який стан активності та soft-delete).
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти категорію за `id`
  2. Повернути `CategoryDto` або 404
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / category detail (planned)
  - API-модуль: —
  - Статус: `partial`
- **Приймає:** path `id` (long).
- **Повертає:** **200** `CategoryDto`.
- **Помилки:** **404** `Category not found`.

### `POST /admin/categories`

- **Summary (1 рядок):** Створення нової категорії.
- **Призначення:** додати категорію до каталогу.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Валідувати `CreateCategoryRequest`
  2. Створити категорію в БД
  3. Повернути `CategoryDto`
- **Side effects (синхронно):** нова категорія; можливий вплив на публічний `GET /catalog/categories` (лише активні)
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / create category (planned)
  - API-модуль: —
  - Статус: `partial`
- **Приймає:** body `CreateCategoryRequest`.
- **Повертає:** створений `CategoryDto` (`id` — auto-increment БД).

### `PUT /admin/categories/{id}`

- **Summary (1 рядок):** Оновлення категорії.
- **Призначення:** змінити дані категорії за id.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти категорію за `id`
  2. Застосувати `UpdateCategoryRequest`
  3. Повернути оновлений `CategoryDto`
- **Side effects (синхронно):** оновлення запису категорії
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / edit category (planned)
  - API-модуль: —
  - Статус: `partial`
- **Приймає:** path `id` (long); body `UpdateCategoryRequest`.
- **Повертає:** оновлений `CategoryDto`.

### `POST /admin/categories/{id}/activate`

- **Summary (1 рядок):** Активація категорії для публічного каталогу.
- **Призначення:** зробити категорію видимою на вітрині.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти категорію
  2. Встановити активний стан
  3. Інвалідувати кеш публічних категорій
- **Side effects (синхронно):** категорія стає видимою в публічному каталозі (якщо не deleted)
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / activate category (planned)
  - API-модуль: —
  - Статус: `partial`
- **Повертає:** **200** порожнє тіло.

### `POST /admin/categories/{id}/deactivate`

- **Summary (1 рядок):** Деактивація категорії.
- **Призначення:** приховати категорію з публічного списку.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти категорію
  2. Деактивувати
  3. Інвалідувати кеш публічних категорій
- **Side effects (синхронно):** прибирається з публічного списку активних
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / deactivate category (planned)
  - API-модуль: —
  - Статус: `partial`
- **Повертає:** **200** порожнє тіло.

### `DELETE /admin/categories/{id}`

- **Summary (1 рядок):** М'яке видалення категорії.
- **Призначення:** soft-delete категорії за id.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: —
- **Бізнес-логіка:**
  1. Знайти категорію
  2. Виконати soft-delete
- **Side effects (синхронно):** soft-delete категорії
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: AdminShell / delete category (planned)
  - API-модуль: —
  - Статус: `partial`
- **Повертає:** **200** порожнє тіло.

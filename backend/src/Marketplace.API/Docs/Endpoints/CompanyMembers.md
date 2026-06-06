# CompanyMembersController — `/companies/{companyId}/members`

Усі маршрути: **`[Authorize]`** + Bearer.

### Правила з `CompanyPermissions`

- **Керування членами** (список усіх, призначення ролі, зміна ролі, видалення): актор — **Owner** або **Manager** у цій компанії, або глобальний **Admin**.
- **Cross-company isolation:** membership у чужій компанії не дає прав на операції в поточній (`Forbidden`).
- **Перегляд своєї ролі** (`GET .../me`): будь-який **автентифікований** користувач; якщо не член — помилка з текстом на кшталт **`Membership not found`**.

### Інваріанти

- Не можна **змінити роль** або **видалити** останнього **Owner** (handlers `ChangeCompanyMemberRole`, `RemoveCompanyMember`).
- **Owner role escalation**: non-admin actor не може призначати/підвищувати до ролі **Owner**.
- Soft-deleted member не враховується в permission checks та listing.
- Роль у body: рядок, парситься в `CompanyMembershipRole` case-insensitive: `owner`, `manager`, `seller`, `support`, `logistics`.

---

## `GET /companies/{companyId}/members`

- **Summary (1 рядок):** Список усіх членів компанії з ролями.
- **Призначення:** список усіх членів компанії з ролями.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin** (bypass)
  - Компанійні ролі: **Owner**, **Manager**
- **Бізнес-логіка:**
  1. Перевірити права Owner/Manager/Admin
  2. Завантажити активних членів компанії
  3. Повернути масив `CompanyMemberDto`
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Workspace members (planned UI)
  - API-модуль: `frontend/src/features/workspace/api/members.api.ts`
  - Статус: `partial`
- **Повертає:** масив `CompanyMemberDto` (`companyId`, `userId`, `role`, `isOwner`, `createdAt`, `updatedAt`).

## `GET /companies/{companyId}/members/me`

- **Summary (1 рядок):** Роль поточного користувача в компанії.
- **Призначення:** роль поточного користувача в цій компанії.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: будь-який автентифікований
  - Компанійні ролі: член компанії (інакше `Membership not found`)
- **Бізнес-логіка:**
  1. Визначити `userId` з JWT
  2. Знайти членство в компанії
  3. Повернути `CompanyMemberDto` або 404
- **Side effects (синхронно):** —
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Workspace context / permissions
  - API-модуль: `frontend/src/features/workspace/api/members.api.ts`
  - Статус: `partial`
- **Повертає:** **200** `CompanyMemberDto` якщо членство є.
- **Помилки:** **`Membership not found`** → у `ResultExtensions` текст містить `not found`, тому зазвичай **404** `ProblemDetails`.

## `POST /companies/{companyId}/members/{userId}/role`

- **Summary (1 рядок):** Призначення ролі члену (upsert членства).
- **Призначення:** призначити роль (upsert членства).
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin** (bypass)
  - Компанійні ролі: **Owner**, **Manager**
- **Бізнес-логіка:**
  1. Перевірити права та інваріант Owner escalation
  2. Створити або оновити `CompanyMember`
  3. Повернути `CompanyMemberDto`
- **Side effects (синхронно):** створення/оновлення `CompanyMember`
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Workspace members invite (planned)
  - API-модуль: `frontend/src/features/workspace/api/members.api.ts`
  - Статус: `partial`
- **Приймає:** body `{ "role": "..." }` (`CompanyMemberRoleRequest`).
- **Повертає:** **200** `CompanyMemberDto`.
- **Помилки:** **400** невалідна роль; `Company not found`; `User not found`; `Forbidden` (в т.ч. спроба non-admin призначити `Owner`).

## `PATCH /companies/{companyId}/members/{userId}/role`

- **Summary (1 рядок):** Зміна ролі існуючого члена.
- **Призначення:** змінити роль існуючого члена.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin** (bypass)
  - Компанійні ролі: **Owner**, **Manager**
- **Бізнес-логіка:**
  1. Перевірити права та інваріант останнього Owner
  2. Оновити роль члена
  3. Повернути `CompanyMemberDto`
- **Side effects (синхронно):** оновлення ролі
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Workspace members (planned)
  - API-модуль: `frontend/src/features/workspace/api/members.api.ts`
  - Статус: `partial`
- **Повертає:** **200** `CompanyMemberDto`.
- **Помилки:** інваріант останнього Owner; **400** невалідна роль; `Forbidden` для non-admin при зміні на `Owner`.

## `DELETE /companies/{companyId}/members/{userId}`

- **Summary (1 рядок):** Видалення членства (soft-delete).
- **Призначення:** видалити членство (soft-delete).
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin** (bypass)
  - Компанійні ролі: **Owner**, **Manager**
- **Бізнес-логіка:**
  1. Перевірити права та інваріант останнього Owner
  2. Виконати soft-delete членства
- **Side effects (синхронно):** користувач втрачає доступ до ресурсів компанії
- **Async / «магія»:** —
- **Де на фронті:**
  - Екран: Workspace members (planned)
  - API-модуль: `frontend/src/features/workspace/api/members.api.ts`
  - Статус: `partial`
- **Повертає:** **200** порожнє тіло при успіху.
- **Помилки:** не можна видалити останнього Owner.

## Observability

- Метрики: `company_operations_total`, `company_errors_total`, `company_latency_ms`.
- Ключові operations labels: `company_members_list`, `company_member_me`, `company_member_assign_role`, `company_member_change_role`, `company_member_remove`.

## Примітка (архітектура API)

Зараз модель — **одна роль на пару (користувач, компанія)**. Розширення на multi-role описано як майбутня міграція в старому README; при зміні контракту оновити цей файл.

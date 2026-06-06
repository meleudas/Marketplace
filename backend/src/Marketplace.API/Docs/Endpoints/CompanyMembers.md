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

- **Призначення:** список усіх членів компанії з ролями.
- **Повертає:** масив `CompanyMemberDto` (`companyId`, `userId`, `role`, `isOwner`, `createdAt`, `updatedAt`).
- **Авторизація:** **Owner | Manager | Admin** (іншим — `Forbidden` у handler).
- **Side effects:** немає.

## `GET /companies/{companyId}/members/me`

- **Призначення:** роль поточного користувача в цій компанії.
- **Повертає:** **200** `CompanyMemberDto` якщо членство є.
- **Помилки:** **`Membership not found`** → у `ResultExtensions` текст містить `not found`, тому зазвичай **404** `ProblemDetails`.

## `POST /companies/{companyId}/members/{userId}/role`

- **Призначення:** призначити роль (upsert членства).
- **Приймає:** body `{ "role": "..." }` (`CompanyMemberRoleRequest`).
- **Повертає:** **200** `CompanyMemberDto`.
- **Авторизація:** Owner | Manager | Admin.
- **Side effects:** створення/оновлення `CompanyMember`.
- **Помилки:** **400** невалідна роль; `Company not found`; `User not found`; `Forbidden` (в т.ч. спроба non-admin призначити `Owner`).

## `PATCH /companies/{companyId}/members/{userId}/role`

- **Призначення:** змінити роль існуючого члена.
- **Повертає:** **200** `CompanyMemberDto`.
- **Авторизація:** Owner | Manager | Admin.
- **Side effects:** оновлення ролі.
- **Помилки:** інваріант останнього Owner; **400** невалідна роль; `Forbidden` для non-admin при зміні на `Owner`.

## `DELETE /companies/{companyId}/members/{userId}`

- **Призначення:** видалити членство (soft-delete).
- **Повертає:** **200** порожнє тіло при успіху.
- **Авторизація:** Owner | Manager | Admin.
- **Side effects:** користувач втрачає доступ до ресурсів компанії.
- **Помилки:** не можна видалити останнього Owner.

## Observability

- Метрики: `company_operations_total`, `company_errors_total`, `company_latency_ms`.
- Ключові operations labels: `company_members_list`, `company_member_me`, `company_member_assign_role`, `company_member_change_role`, `company_member_remove`.

## Примітка (архітектура API)

Зараз модель — **одна роль на пару (користувач, компанія)**. Розширення на multi-role описано як майбутня міграція в старому README; при зміні контракту оновити цей файл.

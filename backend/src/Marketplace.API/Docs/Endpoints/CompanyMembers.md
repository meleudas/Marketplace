# CompanyMembersController — `/companies/{companyId}/members`

Усі маршрути: **`[Authorize]`** + Bearer.

### Правила з `CompanyPermissions`

- **Керування членами** (список усіх, призначення ролі, зміна ролі, видалення): актор — **Owner** або **Manager** у цій компанії, або глобальний **Admin**.
- **Перегляд своєї ролі** (`GET .../me`): будь-який **автентифікований** користувач; якщо не член — помилка з текстом на кшталт **`Membership not found`**.

### Інваріанти

- Не можна **змінити роль** або **видалити** останнього **Owner** (handlers `ChangeCompanyMemberRole`, `RemoveCompanyMember`).
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
- **Помилки:** **400** невалідна роль; `Company not found`; `User not found`; `Forbidden`.

## `PATCH /companies/{companyId}/members/{userId}/role`

- **Призначення:** змінити роль існуючого члена.
- **Повертає:** **200** `CompanyMemberDto`.
- **Авторизація:** Owner | Manager | Admin.
- **Side effects:** оновлення ролі.
- **Помилки:** інваріант останнього Owner; **400** невалідна роль.

## `DELETE /companies/{companyId}/members/{userId}`

- **Призначення:** видалити членство (soft-delete).
- **Повертає:** **200** порожнє тіло при успіху.
- **Авторизація:** Owner | Manager | Admin.
- **Side effects:** користувач втрачає доступ до ресурсів компанії.
- **Помилки:** не можна видалити останнього Owner.

## Примітка (архітектура API)

Зараз модель — **одна роль на пару (користувач, компанія)**. Розширення на multi-role описано як майбутня міграція в старому README; при зміні контракту оновити цей файл.

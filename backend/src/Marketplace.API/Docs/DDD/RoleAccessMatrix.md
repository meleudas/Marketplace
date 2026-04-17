# Матриця доступів за ролями

## Глобальні ролі маркетплейсу (`UserRole`)

Зберігаються в профілі/Identity; у JSON зазвичай у **нижньому регістрі** (`buyer`, `seller`, `moderator`, `admin`).

| Роль | Типове призначення | Доступ |
|------|-------------------|--------|
| **Buyer** | Покупець | Публічний каталог; автентифіковані `/users/me` тощо |
| **Seller** | Продавець (глобальний маркер) | Як buyer + залежить від **компанійного** членства для B2B-операцій |
| **Moderator** | Модерація (зарезервовано продуктом) | За замовчуванням ті самі ендпоінти, що й звичайний user, якщо немає окремих політик |
| **Admin** | Адміністратор платформи | **Повний доступ** до `/admin/*`; **bypass** перевірок членства компанії для `/companies/{id}/products`, `/companies/{id}/inventory/*`, `/companies/{id}/members/*`; `PATCH /users/{id}/role` |

### Явні адмін-маршрути

- Усі **`/admin/*`** — тільки **`Admin`** ([Endpoints/AdminCatalog.md](../Endpoints/AdminCatalog.md)).

### Зміна глобальної ролі

- **`PATCH /users/{id}/role`** — лише **`Admin`** ([Endpoints/Users.md](../Endpoints/Users.md)).

---

## Компанійні ролі (`CompanyMembershipRole`)

Одна первинна роль на пару **(користувач, компанія)**. Значення в API: `owner`, `manager`, `seller`, `support`, `logistics` (case-insensitive).

### Доступ до товарів компанії (`ProductAccessService`)

| Роль | `GET .../products` (ReadInternal) | `POST/PUT/DELETE .../products` (WriteProduct) |
|------|-----------------------------------|---------------------------------------------|
| Owner | Так | Так |
| Manager | Так | Так |
| Seller | Так | Так |
| Support | Так | Ні |
| Logistics | Так | Ні |
| Не член | Ні | Ні |
| **Admin** (глобально) | Так | Так |

Джерело: `Marketplace.Application/Products/Authorization/ProductAccessService.cs`.

### Доступ до складу та інвентарю (`InventoryAccessService`)

| Роль | Читання складів, стоків, рухів (ReadInternal) | Операції receive/ship/adjust/transfer/reserve/release, CRUD складу (WriteStock) |
|------|-----------------------------------------------|----------------------------------------------------------------------------------|
| Owner | Так | Так |
| Manager | Так | Так |
| Logistics | Так | Так |
| Seller | Так | Ні |
| Support | Так | Ні |
| Не член | Ні | Ні |
| **Admin** | Так | Так |

Джерело: `Marketplace.Application/Inventory/Authorization/InventoryAccessService.cs`.

### Керування членами компанії (`CompanyPermissions.CanManageMembers`)

| Роль | `GET .../members` (список) | `POST/PATCH .../role`, `DELETE .../members/{userId}` |
|------|----------------------------|------------------------------------------------------|
| Owner | Так | Так |
| Manager | Так | Так |
| Seller | Ні | Ні |
| Support | Ні | Ні |
| Logistics | Ні | Ні |
| **Admin** | Так | Так |

Джерело: `GetCompanyMembersQueryHandler`, команди assign/change/remove + `CompanyPermissions.cs`.

### Перегляд своєї ролі

- **`GET .../members/me`** — будь-який автентифікований користувач; якщо не член — **404** з текстом про membership ([Endpoints/CompanyMembers.md](../Endpoints/CompanyMembers.md)).
- **`GET /users/me`** — у відповіді `companyMemberships`: усі компанії, де користувач має активне членство (назва, slug, роль у компанії); зручно для навігації без окремого виклику на кожну компанію.

---

## Інші маршрути

| Група | Хто |
|-------|-----|
| `/auth/*`, `/account/*` (крім публічних confirm/forgot) | За описом кожного методу ([Endpoints/Auth.md](../Endpoints/Auth.md), [Endpoints/Account.md](../Endpoints/Account.md)) |
| `/users/*` | Будь-який Bearer; **`PATCH .../role`** — Admin |
| `/catalog/*` | Анонімно |
| `/integrations/telegram/webhook` | Секрет у заголовку (якщо налаштовано) |

---

## Відображення помилок доступу

- Текст **`Forbidden`** у `Result` → **403** (`ResultExtensions.MapStatus`).
- Текст з **`not found`** → **404** (наприклад `Membership not found`).

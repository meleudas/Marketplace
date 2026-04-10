# Бізнес-потоки (словами)

Короткий опис того, **що відбувається на бекенді** у типових сценаріях. Деталі HTTP — у [Endpoints/README.md](../Endpoints/README.md).

## 1. Реєстрація та перший вхід

1. Клієнт викликає **`POST /auth/register`** — створюється Identity-користувач і доменний профіль; відправляється лист підтвердження.
2. Якщо увімкнено підтвердження email, access/refresh можуть **не** видатись — користувач підтверджує пошту.
3. **`POST /account/confirm-email`** виставляє `EmailConfirmed` і позначає профіль верифікованим.
4. **`POST /auth/login`** видає JWT + refresh-cookie; оновлюється `lastLogin` у профілі.

**Залучені контексти:** [IdentityAndAccess](IdentityAndAccess.md).

---

## 2. Створення компанії та потрапляння у публічний каталог

1. Адмін створює компанію через **`POST /admin/companies`** (або інший флоу онбордингу, якщо з’явиться).
2. Доки **`isApproved = false`**, компанія **не** потрапляє в **`GET /catalog/companies`**.
3. **`POST /admin/companies/{id}/approve`** виставляє апрув і метадані (хто/коли).
4. Публічний фронт читає **`GET /catalog/companies`** — лише схвалені.

**Контексти:** [CompaniesContext](CompaniesContext.md), [CatalogContext](CatalogContext.md).

---

## 3. Запрошення команди та ролі

1. Користувач автентифікований (JWT).
2. **Owner/Manager** (або Admin) викликає **`POST /companies/{companyId}/members/{userId}/role`** з потрібною роллю — створюється або оновлюється `CompanyMember`.
3. Учасник перевіряє **`GET /companies/{companyId}/members/me`** — бачить свою роль або **404**, якщо не доданий.
4. Далі **ті самі** `userId` + `companyId` використовуються в `Products`/`Inventory` для перевірки `ReadInternal` / `Write*`.

**Контексти:** [CompaniesContext](CompaniesContext.md), [RoleAccessMatrix](RoleAccessMatrix.md).

---

## 4. Створення товару та поява на вітрині

1. **Owner/Manager/Seller** (або Admin) викликає **`POST /companies/{companyId}/products`** — продукт зберігається з `slug`, ціною, категорією тощо.
2. Handler скидає кеш вітрини: **`catalog:products:list`** і ключ деталі за slug.
3. Публічний **`GET /catalog/products`** будує список: активні продукти + для кожного підвантажує **залишки по всіх складах** компанії і рахує **`availableQty`** та статус (`out_of_stock` / `low_stock` / `in_stock` за порогами **0 / 5**).
4. Картка **`GET /catalog/products/{slug}`** — той самий принцип наявності + кеш деталі.

**Контексти:** [ProductsContext](ProductsContext.md), [InventoryContext](InventoryContext.md), [CatalogContext](CatalogContext.md).

---

## 5. Оприбуткування та продаж зі складу

1. **Owner/Manager/Logistics** викликає **`POST .../inventory/receive`** з унікальним **`operationId`** — створюється/оновлюється `WarehouseStock`, пишеться `StockMovement`.
2. Повтор з тим самим `operationId` **відхиляється** (ідемпотентність на рівні компанії).
3. Списання — **`POST .../inventory/ship`** за тією ж схемою.
4. Після зміни кількості скидається **`catalog:products:list`**, щоб вітрина не показувала застарілий агрегат 5 хвилин.

**Контексти:** [InventoryContext](InventoryContext.md), [CatalogContext](CatalogContext.md).

---

## 6. Резерв під замовлення

1. **`POST .../inventory/reservations`** з **`reservationCode`** і **`ttlMinutes`** (1..120) збільшує `Reserved` на складі, створює запис резервації і рух типу Reserve.
2. Повторний виклик з тим самим кодом, поки резерв **активний**, повертає успіх **без подвійного списання**.
3. **`DELETE .../reservations/{reservationCode}`** звільняє резерв (логіка в `ReleaseReservationCommandHandler`).

**Контексти:** [InventoryContext](InventoryContext.md).

---

## 7. Глобальна роль адміна платформи

1. Інший Admin викликає **`PATCH /users/{id}/role`** — змінюється **глобальна** роль (`UserRole`).
2. Користувач з роллю **Admin** може заходити в **`/admin/*`** і обходити перевірки членства в **будь-якій** компанії для внутрішніх company-scoped API.

**Контексти:** [IdentityAndAccess](IdentityAndAccess.md), [RoleAccessMatrix](RoleAccessMatrix.md).

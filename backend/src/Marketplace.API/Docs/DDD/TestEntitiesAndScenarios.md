# Тестові сутності та сценарії

## Готовий seed у репозиторії

Повний набір узгоджених даних задається скриптом [backend/scripts/seed-test-data.sql](../../../../scripts/seed-test-data.sql) (запуск: `docker compose --profile tools run --rm db-seed` з кореня репо).  
Пароль **усіх** акаунтів: **`Admin123!`**.

Після міграцій і seed (схема з **uuid** для `companies.Id`):

| Сутність | Значення |
|----------|----------|
| **Tech Store** | `CompanyId` = `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa`, slug `tech-store` |
| **Home Comfort** | `CompanyId` = `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb`, slug `home-comfort` (також **approved**) |
| **Категорії** | `1` Electronics, `2` Smartphones (батько 1), `3` Home |
| **Товари (slug для `/catalog/products/{slug}`)** | `seed-phone-alpha`, `seed-laptop-beta`, `seed-earbuds-gamma`, `seed-kettle-home` |
| **Склади** | Tech: warehouse `1` MAIN-KYIV, `2` SEC-LVIV; Home: `3` HOME-LVIV |
| **Резерв (демо)** | `SEED-RES-DEMO` на товар 1 (склад 1), 5 од. |

Детальний список логінів — у [backend/DOCKER_COMMANDS.md](../../../../DOCKER_COMMANDS.md) (розділ про seed).

## Швидка матриця «хто що тестує»

| Логін | Глобально | У Tech Store | Типові перевірки |
|-------|-----------|--------------|------------------|
| `admin@marketplace.test` | Admin | — | `GET /admin/*`, bypass company API |
| `seller@marketplace.test` | Seller | Owner | members, products write, inventory write |
| `manager@marketplace.test` | Seller | Manager | як Owner для цих API |
| `seller2@marketplace.test` | Seller | Seller | products write; inventory лише read |
| `support@marketplace.test` | Buyer | Support | read products/inventory; **не** `GET .../members` |
| `logistics@marketplace.test` | Buyer | Logistics | inventory write; products read |
| `buyer@marketplace.test` | Buyer | Owner у **Home Comfort** | кабінет другої компанії, товар `seed-kettle-home` |
| `moderator@marketplace.test` | Moderator | — | глобальна роль (залежить від ваших політик на маршрутах) |
| `user@marketplace.test` | User | — | `UserName` у Identity: **plainuser** |

## Покрокові сценарії (happy path)

### A. Вітрина після seed

1. Без логіну: `GET /catalog/companies` — обидві компанії.
2. `GET /catalog/products` — чотири товари з `availableQty` / `availabilityStatus` зі складу.
3. `GET /catalog/products/seed-phone-alpha` — картка з деталлю/зображенням (seed).

### B. Ролі продуктів (Tech Store)

1. `support@` + Bearer: `GET /companies/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/products` → **200**; `POST` → **403**.
2. `seller2@`: `POST .../products` → **200** (якщо body валідний).

### C. Ролі складу

1. `seller2@`: `GET .../inventory/stocks` → **200**; `POST .../inventory/receive` → **403**.
2. `logistics@`: `POST .../inventory/receive` з новим `operationId` → **200**.

### D. Члени компанії

1. `manager@`: `GET .../members` → **200**.
2. `support@`: той самий виклик → **403** (`Forbidden`).
3. `GET .../members/me` для члена → **200**; `buyer@` для Tech Store (не член) → **404** (`Membership not found`).

### E. Інваріант останнього Owner

1. Спроба змінити/видалити єдиного Owner у компанії → помилка з handler.

### F. Ідемпотентність інвентарю

1. Повтор `POST .../receive` з тим самим `operationId` → **400** / текст про вже оброблену операцію.

### G. Резерв

1. У БД уже є активний резерв `SEED-RES-DEMO`; для нового коду — `POST .../reservations` з унікальним `reservationCode`.
2. `DELETE .../reservations/{code}` — зняття резерву.

## Негативні кейси (чекліст)

- Невалідний JWT на `[Authorize]` → **401**.
- Чужий `companyId` або відсутнє членство → **403** на products/inventory.
- `PATCH /users/{id}/role` не адміном → **403** (ASP.NET roles).
- Невалідна компанійна роль у body members → **400**.

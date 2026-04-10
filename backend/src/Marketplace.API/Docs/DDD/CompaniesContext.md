# Bounded context: Companies (організації та членство)

## Призначення

Юридична/організаційна одиниця **Company** на маркетплейсі: реквізити, slug, статус **схвалення адміном**, а також **членство** користувачів з **компанійною роллю**.

## Ключові сутності

- **Company** — `id`, контакти, адреса, `isApproved`, soft-delete.
- **CompanyMember** — зв’язок `userId` ↔ `companyId` + **одна** роль (`Owner`, `Manager`, `Seller`, `Support`, `Logistics`).
- **Модерація** — перехід `isApproved` через адміна (контекст перетинається з **Catalog** для публічного списку).

## Інваріанти

- Не можна залишити компанію без **Owner** (зміна ролі / видалення члена).
- Керування списком членів і ролями — тільки **Owner/Manager** або **Admin**.

## Зв’язки

- **Products / Inventory:** `companyId` у URL — **межа доступу (tenant)**; права всередині компанії залежать від `CompanyMember.Role`.
- **Identity:** `userId` — це той самий guid, що й Identity user id у контролерах.

## HTTP API

- Публічно: схвалені компанії — [Endpoints/Catalog.md](../Endpoints/Catalog.md) (`GET /catalog/companies`).
- Адмін: CRUD + approve/revoke — [Endpoints/AdminCatalog.md](../Endpoints/AdminCatalog.md).
- Членство: [Endpoints/CompanyMembers.md](../Endpoints/CompanyMembers.md).

## Код

- `Marketplace.Application/Companies/**`
- `Marketplace.Application/Companies/Authorization/CompanyPermissions.cs`
- `Marketplace.Domain/Companies/Enums/CompanyMembershipRole.cs`

# Finance endpoints

## `GET /companies/{companyId}/earnings/summary`

- **Summary:** Зведення заробітку продавця (available, pending, fees).
- **Призначення:** dashboard фінансів компанії за період.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: член компанії з доступом до фінансів
- **Query:** `from`, `to` (UTC, optional)
- **Повертає:** `SellerEarningsSummaryDto`.

## `GET /companies/{companyId}/settlements`

- **Summary:** Список settlement batch-ів компанії.
- **Призначення:** історія виплат і статусів batch.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: член компанії
- **Повертає:** `SettlementBatchSummaryDto[]`.

## `PATCH /companies/{companyId}/payout-profile`

- **Summary:** Оновлення IBAN/отримувача для виплат.
- **Призначення:** зберегти реквізити для settlement payout.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
  - Компанійні ролі: owner/manager компанії
- **Body:** `payoutIban?`, `payoutRecipientName?`, `payoutProviderAccountId?`
- **Повертає:** **200** оновлений профіль.

## `GET /admin/settlements`

- **Summary:** Адмін-список settlement batch-ів.
- **Призначення:** модерація та обробка виплат продавцям.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
- **Query:** `status?`, `companyId?`
- **Повертає:** paginated `SettlementBatchAdminDto[]`.

## `GET /admin/companies/{companyId}/commission-rates`

- **Summary:** Історія комісійних ставок компанії.
- **Призначення:** перегляд версій commission rate.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
- **Повертає:** `CompanyCommissionRateDto[]`.

## `POST /admin/settlements/{batchId}/approve-payout`

- **Summary:** Схвалити виплату batch (Ready → Approved).
- **Призначення:** admin підтверджує payout перед банком.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
- **Повертає:** **200** оновлений batch.

## `POST /admin/settlements/{batchId}/mark-paid`

- **Summary:** Позначити batch як оплачений.
- **Призначення:** зафіксувати bank reference після переказу.
- **Хто може викликати:**
  - JWT: обов'язково
  - Глобальні ролі: **Admin**
- **Body:** `bankReference`
- **Повертає:** **200** оновлений batch.

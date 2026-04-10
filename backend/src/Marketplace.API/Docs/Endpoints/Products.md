# ProductsController — `/companies/{companyId}/products`

Усі маршрути: **`[Authorize]`** + Bearer.

### Матриця доступу (з `ProductAccessService`)

| Дія | Умова |
|-----|--------|
| Будь-який запит | Користувач — член компанії (`CompanyMember`) **або** глобальний **Admin** |
| Читання (`ReadInternal`) | Будь-який член компанії |
| Створення/оновлення/видалення (`WriteProduct`) | Роль члена: **Owner**, **Manager**, **Seller** |
| Admin | Bypass перевірки членства |

## `GET /companies/{companyId}/products`

- **Призначення:** внутрішній список товарів компанії з інформацією про наявність (для кабінету продавця).
- **Приймає:** path `companyId` (guid).
- **Повертає:** колекція DTO з handler-а `GetCompanyProductsQuery` (внутрішній перегляд + availability).
- **Авторизація:** `ReadInternal` (будь-який член або Admin).
- **Помилки:** **Forbidden** якщо не член і не Admin.

## `POST /companies/{companyId}/products`

- **Призначення:** створити товар компанії (без окремого адмін-апруву товару).
- **Приймає:** body `UpsertProductRequest` (`name`, `slug`, `description`, `price`, `oldPrice?`, `minStock`, `categoryId`, `hasVariants`, `detail?`, `images?`).
- **Повертає:** **200** `ProductDto` (створений товар з деталями/зображеннями за наявності).
- **Авторизація:** `WriteProduct`.
- **Side effects:** запис продукту, деталей, зображень; **інвалідація кешу:** `catalog:products:list` та `catalog:products:detail:{slug}`.

## `PUT /companies/{companyId}/products/{id}`

- **Приймає:** path `companyId`, `id` (long); body `UpsertProductRequest`.
- **Повертає:** **200** оновлений `ProductDto`.
- **Авторизація:** `WriteProduct`.
- **Side effects:** оновлення сутності; **інвалідація кешу** для старого та нового `slug` + список.

## `DELETE /companies/{companyId}/products/{id}`

- **Призначення:** soft-delete товару.
- **Повертає:** **200** порожнє тіло при успіху.
- **Авторизація:** `WriteProduct`.
- **Side effects:** товар прихований з вітрини; **інвалідація кешу** списку та деталі за старим slug.

## Типові помилки

- `Forbidden` — немає членства або недостатньо ролі для запису.
- `Product not found` — невірний `id` або чужий `companyId` (залежно від handler).
- Помилки валідації slug/категорії — згідно command validator-ів.

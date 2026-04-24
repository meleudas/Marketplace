# ReviewsController — `/products/*/reviews`, `/companies/*/reviews`, `/reviews/*`, `/admin/reviews/*`

### `GET /products/{productId}/reviews?page=&size=`
- **Призначення:** публічний список відгуків товару з пагінацією.
- **Авторизація:** `AllowAnonymous`.
- **Повертає:** `ReviewListDto`.
- **Side effects:** читає з кешу `catalog:products:reviews:{productId}:{page}:{size}`.

### `POST /products/{productId}/reviews`
- **Призначення:** створити відгук на товар.
- **Авторизація:** `[Authorize]`.
- **Інваріанти:** лише verified buyer; 1 відгук на `(productId, userId)`.
- **Повертає:** `ReviewDto`.
- **Side effects:** перерахунок `rating/reviewCount` товару + інвалідація catalog/review кешу.

### `PATCH /products/{productId}/reviews/{reviewId}`
- **Призначення:** оновити власний відгук на товар.
- **Авторизація:** `[Authorize]`.
- **Повертає:** `ReviewDto`.

### `DELETE /products/{productId}/reviews/{reviewId}`
- **Призначення:** soft-delete власного відгуку.
- **Авторизація:** `[Authorize]`.
- **Повертає:** `200 OK`.

### `GET /companies/{companyId}/reviews?page=&size=`
- **Призначення:** публічний список відгуків компанії.
- **Авторизація:** `AllowAnonymous`.
- **Повертає:** `ReviewListDto`.
- **Side effects:** читає з кешу `catalog:companies:reviews:{companyId}:{page}:{size}`.

### `POST /companies/{companyId}/reviews`
- **Призначення:** створити відгук на компанію.
- **Авторизація:** `[Authorize]`.
- **Інваріанти:** лише verified buyer; 1 відгук на `(companyId, userId)`.
- **Повертає:** `ReviewDto`.

### `PATCH /companies/{companyId}/reviews/{reviewId}`
- **Призначення:** оновити власний відгук на компанію.
- **Авторизація:** `[Authorize]`.
- **Повертає:** `ReviewDto`.

### `DELETE /companies/{companyId}/reviews/{reviewId}`
- **Призначення:** soft-delete власного відгуку на компанію.
- **Авторизація:** `[Authorize]`.
- **Повертає:** `200 OK`.

### `PUT /reviews/products/{reviewId}/reply`
- **Призначення:** створити/редагувати офіційну відповідь продавця на відгук товару.
- **Авторизація:** `[Authorize]`.
- **Доступ:** `Owner|Manager|Seller` компанії товару або `Admin`.
- **Повертає:** `ReviewReplyDto`.

### `PUT /reviews/companies/{reviewId}/reply`
- **Призначення:** створити/редагувати офіційну відповідь продавця на відгук компанії.
- **Авторизація:** `[Authorize]`.
- **Доступ:** `Owner|Manager|Seller` або `Admin`.
- **Повертає:** `ReviewReplyDto`.

### `POST /admin/reviews/products/{reviewId}/moderate`
- **Призначення:** модерація відгуку на товар.
- **Авторизація:** `[Authorize(Roles = "Admin,Moderator")]`.
- **Повертає:** `ReviewDto`.

### `POST /admin/reviews/companies/{reviewId}/moderate`
- **Призначення:** модерація відгуку на компанію.
- **Авторизація:** `[Authorize(Roles = "Admin,Moderator")]`.
- **Повертає:** `ReviewDto`.

# ReviewsController — `/products/*/reviews`, `/companies/*/reviews`, `/reviews/*`, `/admin/reviews/*`

### `GET /products/{productId}/reviews?page=&size=`
- **Призначення:** публічний список відгуків товару з пагінацією.
- **Авторизація:** `AllowAnonymous`.
- **Повертає:** `ReviewListDto`.
- **Side effects:** читає з кешу `catalog:products:reviews:{productId}:{page}:{size}`.
- **Metrics:** `review_latency_ms{operation="reviews_list_product"}`, `review_operations_total{operation="reviews_list_product",status="success"}`, `review_errors_total{operation="reviews_list_product",reason=*}`.

### `POST /products/{productId}/reviews`
- **Призначення:** створити відгук на товар.
- **Авторизація:** `[Authorize]`.
- **Інваріанти:** лише verified buyer; 1 відгук на `(productId, userId)`.
- **Повертає:** `ReviewDto`.
- **Side effects:** перерахунок `rating/reviewCount` товару + інвалідація catalog/review кешу.
- **Metrics:** `review_latency_ms{operation="reviews_create_product"}`, `review_operations_total{operation="reviews_create_product",status="success"}`, `review_errors_total{operation="reviews_create_product",reason="unauthorized|forbidden|not_found|application_failure"}`.

### `PATCH /products/{productId}/reviews/{reviewId}`
- **Призначення:** оновити власний відгук на товар.
- **Авторизація:** `[Authorize]`.
- **Повертає:** `ReviewDto`.
- **Metrics:** `review_latency_ms{operation="reviews_update_product"}`, `review_operations_total{operation="reviews_update_product",status="success"}`, `review_errors_total{operation="reviews_update_product",reason="unauthorized|forbidden|not_found|application_failure"}`.

### `DELETE /products/{productId}/reviews/{reviewId}`
- **Призначення:** soft-delete власного відгуку.
- **Авторизація:** `[Authorize]`.
- **Повертає:** `200 OK`.
- **Metrics:** `review_latency_ms{operation="reviews_delete_product"}`, `review_operations_total{operation="reviews_delete_product",status="success"}`, `review_errors_total{operation="reviews_delete_product",reason="unauthorized|forbidden|not_found|application_failure"}`.

### `GET /companies/{companyId}/reviews?page=&size=`
- **Призначення:** публічний список відгуків компанії.
- **Авторизація:** `AllowAnonymous`.
- **Повертає:** `ReviewListDto`.
- **Side effects:** читає з кешу `catalog:companies:reviews:{companyId}:{page}:{size}`.
- **Metrics:** `review_latency_ms{operation="reviews_list_company"}`, `review_operations_total{operation="reviews_list_company",status="success"}`, `review_errors_total{operation="reviews_list_company",reason=*}`.

### `POST /companies/{companyId}/reviews`
- **Призначення:** створити відгук на компанію.
- **Авторизація:** `[Authorize]`.
- **Інваріанти:** лише verified buyer; 1 відгук на `(companyId, userId)`.
- **Повертає:** `ReviewDto`.
- **Metrics:** `review_latency_ms{operation="reviews_create_company"}`, `review_operations_total{operation="reviews_create_company",status="success"}`, `review_errors_total{operation="reviews_create_company",reason="unauthorized|forbidden|not_found|application_failure"}`.

### `PATCH /companies/{companyId}/reviews/{reviewId}`
- **Призначення:** оновити власний відгук на компанію.
- **Авторизація:** `[Authorize]`.
- **Повертає:** `ReviewDto`.
- **Metrics:** `review_latency_ms{operation="reviews_update_company"}`, `review_operations_total{operation="reviews_update_company",status="success"}`, `review_errors_total{operation="reviews_update_company",reason="unauthorized|forbidden|not_found|application_failure"}`.

### `DELETE /companies/{companyId}/reviews/{reviewId}`
- **Призначення:** soft-delete власного відгуку на компанію.
- **Авторизація:** `[Authorize]`.
- **Повертає:** `200 OK`.
- **Metrics:** `review_latency_ms{operation="reviews_delete_company"}`, `review_operations_total{operation="reviews_delete_company",status="success"}`, `review_errors_total{operation="reviews_delete_company",reason="unauthorized|forbidden|not_found|application_failure"}`.

### `PUT /reviews/products/{reviewId}/reply`
- **Призначення:** створити/редагувати офіційну відповідь продавця на відгук товару.
- **Авторизація:** `[Authorize]`.
- **Доступ:** `Owner|Manager|Seller` компанії товару або `Admin`.
- **Повертає:** `ReviewReplyDto`.
- **Metrics:** `review_latency_ms{operation="reviews_reply_product"}`, `review_operations_total{operation="reviews_reply_product",status="success"}`, `review_errors_total{operation="reviews_reply_product",reason="unauthorized|forbidden|not_found|application_failure"}`.

### `PUT /reviews/companies/{reviewId}/reply`
- **Призначення:** створити/редагувати офіційну відповідь продавця на відгук компанії.
- **Авторизація:** `[Authorize]`.
- **Доступ:** `Owner|Manager|Seller` або `Admin`.
- **Повертає:** `ReviewReplyDto`.
- **Metrics:** `review_latency_ms{operation="reviews_reply_company"}`, `review_operations_total{operation="reviews_reply_company",status="success"}`, `review_errors_total{operation="reviews_reply_company",reason="unauthorized|forbidden|not_found|application_failure"}`.

### `POST /admin/reviews/products/{reviewId}/moderate`
- **Призначення:** модерація відгуку на товар.
- **Авторизація:** `[Authorize(Roles = "Admin,Moderator")]`.
- **Повертає:** `ReviewDto`.
- **Metrics:** `review_latency_ms{operation="reviews_moderate_product"}`, `review_operations_total{operation="reviews_moderate_product",status="success"}`, `review_errors_total{operation="reviews_moderate_product",reason="unauthorized|forbidden|not_found|application_failure"}`.

### `POST /admin/reviews/companies/{reviewId}/moderate`
- **Призначення:** модерація відгуку на компанію.
- **Авторизація:** `[Authorize(Roles = "Admin,Moderator")]`.
- **Повертає:** `ReviewDto`.
- **Metrics:** `review_latency_ms{operation="reviews_moderate_company"}`, `review_operations_total{operation="reviews_moderate_company",status="success"}`, `review_errors_total{operation="reviews_moderate_company",reason="unauthorized|forbidden|not_found|application_failure"}`.

## Типові помилки

- `401 Unauthorized` — відсутній або невалідний `sub` claim в authorize-запитах.
- `403 Forbidden` — недостатні права на update/delete/reply/moderate.
- `404 Not found` — review/product/company не знайдені або route scope не збігається з review target.
- `400`/`application_failure` — порушені доменні інваріанти (rating/comment/verified purchase).

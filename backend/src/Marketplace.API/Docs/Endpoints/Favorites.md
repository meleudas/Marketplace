# Favorites Controller (`/me/favorites`)

## `GET /me/favorites`
- **Призначення:** отримати список улюблених товарів поточного користувача.
- **Приймає:** без параметрів.
- **Повертає:** масив `FavoriteItemDto`.
- **Авторизація:** `[Authorize]`.
- **Кеш:** key `favorites:user:{userId}:list`, TTL **3 хв**.
- **Observability labels:** `favorites_list`.
- **Помилки:** `401` (без токена), `400` (валідація/pipeline).

## `POST /me/favorites/{productId}`
- **Призначення:** додати товар в улюблене (ідемпотентно).
- **Приймає:** `productId` у route.
- **Повертає:** `FavoriteItemDto` (новий або вже існуючий запис).
- **Авторизація:** `[Authorize]`.
- **Ідемпотентність:** повторний POST повертає той самий активний запис; після soft-delete запис реактивується.
- **Кеш:** інвалідує `favorites:user:{userId}:list`.
- **Observability labels:** `favorites_add`.
- **Помилки:** `404` (`Product not found`), `400`.

## `DELETE /me/favorites/{productId}`
- **Призначення:** прибрати товар з улюбленого (ідемпотентно).
- **Приймає:** `productId` у route.
- **Повертає:** `true`.
- **Авторизація:** `[Authorize]`.
- **Side effects:** soft-delete існуючого запису; повторний виклик успішний.
- **Кеш:** інвалідує `favorites:user:{userId}:list`.
- **Observability labels:** `favorites_remove`.

## Observability

- Метрики: `favorites_operations_total`, `favorites_errors_total`, `favorites_latency_ms`.
- Основні operations labels: `favorites_list`, `favorites_add`, `favorites_remove`.

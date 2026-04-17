# Cart Controller (`/me/cart`)

## `GET /me/cart`
- **Призначення:** отримати активний кошик поточного користувача.
- **Приймає:** без параметрів.
- **Повертає:** `CartDto` з item-ами, total item count та total amount.
- **Авторизація:** `[Authorize]`.
- **Side effects:** якщо активний кошик відсутній, створюється порожній.
- **Кеш:** key `cart:user:{userId}:active`, TTL **60s**.
- **Помилки:** `401` (без токена), `400` (валідація/pipeline).

## `POST /me/cart/items`
- **Призначення:** додати товар у кошик.
- **Приймає (body):**
  - `productId: long`
  - `quantity: int`
- **Повертає:** оновлений `CartDto`.
- **Авторизація:** `[Authorize]`.
- **Side effects:** якщо item уже існує для цього товару, кількість інкрементується.
- **Кеш:** інвалідує `cart:user:{userId}:active`.
- **Помилки:** `404` (`Product not found`), `400` (валідація).

## `PATCH /me/cart/items/{itemId}`
- **Призначення:** оновити кількість позиції кошика.
- **Приймає:** `itemId` у route, `quantity` у body.
- **Повертає:** оновлений `CartDto`.
- **Авторизація:** `[Authorize]`.
- **Кеш:** інвалідує `cart:user:{userId}:active`.
- **Помилки:** `404` (`Cart not found`, `Cart item not found`), `400`.

## `DELETE /me/cart/items/{itemId}`
- **Призначення:** видалити позицію з кошика.
- **Приймає:** `itemId` у route.
- **Повертає:** оновлений `CartDto`.
- **Авторизація:** `[Authorize]`.
- **Side effects:** soft-delete позиції.
- **Кеш:** інвалідує `cart:user:{userId}:active`.
- **Помилки:** `404` (`Cart not found`, `Cart item not found`).

## `DELETE /me/cart`
- **Призначення:** очистити активний кошик.
- **Приймає:** без параметрів.
- **Повертає:** порожній `CartDto`.
- **Авторизація:** `[Authorize]`.
- **Side effects:** soft-delete всіх item-ів активного кошика.
- **Кеш:** інвалідує `cart:user:{userId}:active`.
- **Помилки:** `404` (`Cart not found`).

## `POST /me/cart/checkout`
- **Призначення:** оформити checkout із автосплітом кошика на окремі ордери по `CompanyId`.
- **Приймає (body):**
  - `paymentMethod` (`card` / `payPal` / `bankTransfer` / `cash`)
  - `address`
    - `firstName`, `lastName`, `phone`
    - `street`, `city`, `state`, `postalCode`, `country`
  - `notes` (optional)
- **Повертає:** `CheckoutResultDto` з `createdOrders[]`.
- **Авторизація:** `[Authorize]`.
- **Side effects:**
  - створює **N** ордерів (1 компанія = 1 ордер);
  - для кожного ордера створює `order_items`;
  - фіксує snapshot адреси в `order_addresses`;
  - стартовий статус кожного ордера: `pending`;
  - soft-delete checkout-нутих позицій кошика.
- **Кеш:** інвалідує `cart:user:{userId}:active`.
- **Помилки:** `400` (`Invalid paymentMethod`, валідація), `404/400` (`Cart not found`, `Cart is empty`, `Product ... not found`).

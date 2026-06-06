# Shipping Endpoints

## `GET /me/addresses`
- **Призначення:** отримати збережені адреси поточного користувача.
- **Авторизація:** `[Authorize]`.
- **Повертає:** список `UserAddressDto`.

## `POST /me/addresses`
- **Призначення:** створити адресу користувача.
- **Авторизація:** `[Authorize]`.
- **Приймає (body):**
  - `type` (`shipping` / `billing` / `both`)
  - `isDefault`
  - `firstName`, `lastName`, `phone`
  - `street`, `city`, `state`, `postalCode`, `country`
- **Повертає:** створений `UserAddressDto`.

## `PATCH /me/addresses/{addressId}`
- **Призначення:** оновити адресу користувача.
- **Авторизація:** `[Authorize]`.
- **Повертає:** оновлений `UserAddressDto`.

## `DELETE /me/addresses/{addressId}`
- **Призначення:** видалити адресу користувача (soft-delete).
- **Авторизація:** `[Authorize]`.
- **Повертає:** `200 OK`.

## `POST /me/addresses/{addressId}/default`
- **Призначення:** встановити адресу за замовчуванням.
- **Авторизація:** `[Authorize]`.
- **Інваріант:** тільки одна активна default-адреса на користувача.

## `GET /shipping/methods`
- **Призначення:** отримати доступні методи доставки.
- **Авторизація:** публічний endpoint.
- **Повертає:** список `ShippingMethodDto`.

## `POST /shipping/quote`
- **Призначення:** розрахувати shipping quote і зберегти його snapshot.
- **Авторизація:** `[Authorize]`.
- **Приймає (body):** `shippingMethodId` + контакт/адреса.
- **Повертає:** `ShippingQuoteDto` (`quoteId`, сума, ETA, `expiresAtUtc`).

## `GET /me/shipments`
- **Призначення:** отримати shipment-и поточного користувача.
- **Авторизація:** `[Authorize]`.
- **Повертає:** список `ShipmentDto`.

## `POST /integrations/shipping/novaposhta/webhook`
- **Призначення:** прийняти webhook від Nova Poshta.
- **Авторизація:** `AllowAnonymous` (інтеграційний endpoint).
- **Idempotency:** dedup за `(carrierCode, eventKey, payloadHash)`.
- **Приймає:** довільний JSON payload + заголовок `X-NovaPoshta-Event-Id` (optional).

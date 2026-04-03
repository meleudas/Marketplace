# Повна структура БД Marketplace (PostgreSQL + JSONB)

Оновлена версія схеми: **MongoDB повністю прибрано**, всі документні дані перенесено в **PostgreSQL (`JSONB`)** без втрати логіки.  
Redis залишається для кешу/епемерних станів, S3/Blob — для файлів.

---

## 📍 Легенда

| Іконка | Зберігання |
|--------|------------|
| 🟦 | PostgreSQL (реляційні + JSONB) |
| 🟥 | Redis (кеш, сесії, лічильники, короткі стани) |
| 🟧 | Object Storage (S3/Blob файли) |

---

## Глобальне правило soft delete

Для **кожної PostgreSQL-моделі** (усіх таблиць, описаних у цьому документі) додаються стандартні поля:

- `deleted_at` timestamptz nullable
- `is_deleted` bool default `false`

Примітка: навіть якщо поле не дублюється у конкретному списку колонок нижче, воно вважається обов'язковим за цим правилом.

---

## 1. Користувачі та авторизація

### 🟦 `AspNetUsers` (узгоджено з кодом)
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` uuid PK
- `user_name` text unique
- `email` text unique
- `email_confirmed` bool
- `password_hash` text
- `security_stamp` text
- `phone_number` text
- `phone_number_confirmed` bool
- `two_factor_enabled` bool
- `lockout_end_utc` timestamptz nullable
- `lockout_enabled` bool
- `access_failed_count` int
- `telegram_chat_id` varchar(64) nullable
- `telegram_two_factor_enabled` bool default `false`
- `telegram_linked_at_utc` timestamptz nullable
- `created_at` timestamptz
- `updated_at` timestamptz
- `deleted_at` timestamptz nullable
- `is_deleted` bool

### 🟦 `marketplace_users` (узгоджено з кодом)
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` uuid PK, FK -> `AspNetUsers.id`
- `first_name` text
- `last_name` text
- `role` int (мапа enum у застосунку)
- `birthday` date nullable
- `avatar` varchar(2048) nullable
- `is_verified` bool
- `verification_document` varchar(2048) nullable
- `last_login_at` timestamptz nullable
- `created_at` timestamptz
- `updated_at` timestamptz
- `deleted_at` timestamptz nullable
- `is_deleted` bool

### 🟦 `asp_net_roles`, `asp_net_user_roles`, `asp_net_user_claims`, `asp_net_user_logins`, `asp_net_user_tokens`, `asp_net_role_claims`
Стандартні таблиці Identity без змін по логіці.
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false` (для кожної таблиці в блоці)

### 🟥 Redis
- `session:{userId}` -> hash, TTL 30 днів
- `auth:token:{tokenId}` -> string (`userId`), TTL за типом токена
- `chat:online:{userId}` -> string `"1"`, TTL 300 сек

---

## 2. Компанії

### 🟦 `companies`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigint PK
- `name` text
- `slug` text unique
- `description` text
- `image_url` text nullable
- `contact_email` text
- `contact_phone` text
- `country`, `address_region`, `address_city`, `address_street`, `address_postal_code` text
- `is_approved` bool
- `approved_at` timestamptz nullable
- `approved_by_id` text nullable FK -> `users.id`
- `rating` numeric(3,2) nullable (кеш/агрегація)
- `review_count` int default 0
- `follower_count` int default 0
- `meta` jsonb
- `created_at`, `updated_at` timestamptz
- `deleted_at` timestamptz nullable
- `is_deleted` bool

### 🟦 `company_users`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `company_id` bigint FK
- `user_id` text FK
- `is_owner` bool
- `role` smallint/enum (`owner|manager|employee`)
- `permissions` jsonb
- `created_at` timestamptz
- PK (`company_id`, `user_id`)

### 🟦 `company_schedules`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigint PK
- `company_id` bigint FK
- `day_of_week` smallint/enum
- `open_time` time
- `close_time` time
- `is_closed` bool

### 🟦 `company_followers`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigint PK
- `company_id` bigint FK
- `user_id` text FK
- `created_at` timestamptz
- unique (`company_id`, `user_id`)

### 🟦 `company_reviews` (було Mongo)
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigserial PK
- `company_id` bigint FK
- `user_id` uuid FK -> `AspNetUsers.id`
- `user_name` text
- `order_id` bigint nullable FK
- `is_verified_purchase` bool
- `ratings` jsonb  (service, delivery_time, accuracy, communication)
- `overall_rating` numeric(3,2)
- `comment` text
- `response` jsonb nullable (`text`, `responded_at`, `responded_by_id`)
- `status` smallint/enum (`pending|approved|rejected`)
- `moderated_by_id` uuid nullable FK -> `AspNetUsers.id`
- `moderated_at` timestamptz nullable
- `created_at`, `updated_at` timestamptz

Індекси:
- (`company_id`, `created_at` desc)
- (`user_id`)
- (`status`)
- `gin (ratings jsonb_path_ops)`
- `gin (response jsonb_path_ops)`

### 🟥 Redis
- `company:rating:{companyId}` -> hash, TTL 1 година

---

## 3. Категорії

### 🟦 `categories`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigint PK
- `name` text
- `slug` text unique
- `image_url` text nullable
- `parent_id` bigint nullable FK -> `categories.id`
- `description` text nullable
- `meta` jsonb
- `sort_order` int
- `is_active` bool
- `product_count` int default 0
- `created_at`, `updated_at` timestamptz

### 🟥 Redis
- `category:products:{categoryId}` -> list, TTL 30 хв

---

## 4. Товари

### 🟦 `products` (ядро)
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigint PK
- `company_id` bigint FK
- `name` text
- `slug` text unique
- `description` text
- `price` numeric(14,2)
- `old_price` numeric(14,2) nullable
- `stock` int
- `min_stock` int
- `category_id` bigint FK
- `status` smallint/enum (`pending|approved|rejected|archived`)
- `approved_at` timestamptz nullable
- `approved_by_id` text nullable FK -> `users.id`
- `rating` numeric(3,2) nullable
- `review_count` int default 0
- `view_count` bigint default 0
- `sales_count` bigint default 0
- `has_variants` bool
- `created_at`, `updated_at` timestamptz
- `deleted_at` timestamptz nullable
- `is_deleted` bool

### 🟦 `product_details` (було Mongo)
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigserial PK
- `product_id` bigint unique FK -> `products.id`
- `slug` text unique
- `attributes` jsonb
- `variants` jsonb
- `specifications` jsonb
- `seo` jsonb
- `content_blocks` jsonb
- `tags` text[]
- `brands` text[]
- `document_raw` json nullable              // сирий документ після імпорту з Mongo
- `created_at`, `updated_at` timestamptz

Індекси:
- (`product_id`)
- (`slug`)
- `gin (attributes jsonb_path_ops)`
- `gin (variants jsonb_path_ops)`
- `gin (specifications jsonb_path_ops)`
- `gin (seo jsonb_path_ops)`
- `gin (content_blocks jsonb_path_ops)`
- `gin (tags)`
- `gin (brands)`

### 🟦 `product_images`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigint PK
- `product_id` bigint FK
- `image_url` text
- `thumbnail_url` text
- `alt_text` text
- `sort_order` int
- `is_main` bool
- `width` int nullable
- `height` int nullable
- `file_size` bigint nullable
- `created_at` timestamptz
- `deleted_at` timestamptz nullable
- `is_deleted` bool

### 🟧 Object Storage
- `products/{productId}/...`
- `companies/{companyId}/...`
- `categories/{categoryId}/...`
- `reviews/{reviewId}/...`

### 🟥 Redis
- `product:{productId}` -> hash, TTL 1 година
- `product:stock:{productId}` -> string, TTL 5 хв
- `product:views:{productId}` -> counter, TTL 24 години
- `trending:products` -> sorted set, TTL 1 година

---

## 5. Кошик

### 🟦 `carts`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigint PK
- `user_id` text FK
- `status` smallint/enum (`active|abandoned|converted`)
- `last_activity_at` timestamptz
- `created_at`, `updated_at` timestamptz

### 🟦 `cart_items`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigint PK
- `cart_id` bigint FK
- `product_id` bigint FK
- `quantity` int
- `price_at_moment` numeric(14,2)
- `discount` numeric(14,2) default 0
- `created_at`, `updated_at` timestamptz

### 🟥 Redis
- `cart:{userId}` -> hash, TTL 30 днів
- `cart:items:{cartId}` -> hash/json, TTL 30 днів

---

## 6. Замовлення

### 🟦 `orders`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigint PK
- `order_number` text unique
- `customer_id` text FK
- `company_id` bigint FK
- `status` smallint/enum (`pending|paid|processing|shipped|delivered|cancelled|refunded`)
- `total_price`, `subtotal`, `shipping_cost`, `discount_amount`, `tax_amount` numeric(14,2)
- `shipping_method_id` bigint FK
- `payment_method` smallint/enum (`card|paypal|bank_transfer|cash`)
- `notes` text nullable
- `tracking_number` text nullable
- `shipped_at`, `delivered_at`, `cancelled_at`, `refunded_at` timestamptz nullable
- `created_at`, `updated_at` timestamptz

### 🟦 `order_items`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigint PK
- `order_id` bigint FK
- `product_id` bigint FK
- `product_name` text (snapshot)
- `product_image` text nullable (snapshot)
- `quantity` int
- `price_at_moment` numeric(14,2)
- `discount` numeric(14,2)
- `total_price` numeric(14,2)
- `company_id` bigint FK
- `created_at`, `updated_at` timestamptz

### 🟦 `order_status_history`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigint PK
- `order_id` bigint FK
- `old_status` smallint/enum
- `new_status` smallint/enum
- `comment` text nullable
- `changed_by_id` text FK
- `created_at` timestamptz

### 🟦 `order_addresses` (покращення)
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
Незмінний snapshot адрес для історичної точності.
- `id` bigserial PK
- `order_id` bigint FK
- `kind` smallint/enum (`shipping|billing`)
- `first_name`, `last_name`, `street`, `city`, `state`, `postal_code`, `country`, `phone` text
- `created_at` timestamptz
- unique (`order_id`, `kind`)

### 🟥 Redis
- `order:status:{orderId}` -> hash, TTL 7 днів

---

## 7. Доставка

### 🟦 `shipping_methods`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigint PK
- `name` text
- `code` smallint/enum (`nova_poshta|courier|self_pickup|ukrposhta`)
- `price` numeric(14,2)
- `free_shipping_threshold` numeric(14,2) nullable
- `estimated_days_min`, `estimated_days_max` int
- `is_active` bool
- `created_at`, `updated_at` timestamptz

### 🟦 `addresses` (довідник адрес користувача)
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigint PK
- `user_id` text FK
- `type` smallint/enum (`shipping|billing|both`)
- `is_default` bool
- `first_name`, `last_name`, `street`, `city`, `state`, `postal_code`, `country`, `phone` text
- `created_at`, `updated_at` timestamptz
- `deleted_at` timestamptz nullable
- `is_deleted` bool

---

## 8. Платежі

### 🟦 `payments`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigint PK
- `order_id` bigint FK
- `payment_method` smallint/enum (`card|paypal|bank_transfer`)
- `amount` numeric(14,2)
- `currency` text default `UAH`
- `transaction_id` text nullable
- `status` smallint/enum (`pending|completed|failed|refunded`)
- `provider_response` jsonb
- `processed_at` timestamptz nullable
- `created_at`, `updated_at` timestamptz

### 🟦 `refunds`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigint PK
- `payment_id` bigint FK
- `order_id` bigint FK
- `amount` numeric(14,2)
- `reason` text
- `status` smallint/enum (`pending|approved|rejected|completed`)
- `processed_by_id` text FK
- `processed_at` timestamptz nullable
- `created_at`, `updated_at` timestamptz

### 🟥 Redis
- `payment:pending:{paymentId}` -> string, TTL 1 година

---

## 9. Купони та знижки

### 🟦 `coupons`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigint PK
- `code` text unique
- `description` text nullable
- `discount` numeric(14,2)
- `discount_type` smallint/enum (`percentage|fixed`)
- `min_order_amount` numeric(14,2) nullable
- `usage_limit` int nullable
- `usage_count` int default 0
- `user_usage_limit` int default 1
- `expires_at`, `starts_at` timestamptz nullable
- `applicable_categories` jsonb nullable
- `applicable_products` jsonb nullable
- `applicable_companies` jsonb nullable
- `is_active` bool
- `created_at`, `updated_at` timestamptz

### 🟦 `order_coupons`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigint PK
- `order_id` bigint FK
- `coupon_id` bigint FK
- `discount_applied` numeric(14,2)
- `created_at` timestamptz

### 🟥 Redis
- `coupon:code:{code}` -> hash, TTL до `expires_at`

---

## 10. Відгуки про товари (було Mongo)

### 🟦 `product_reviews`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigserial PK
- `product_id` bigint FK -> `products.id`
- `user_id` uuid FK -> `AspNetUsers.id`
- `user_name` text
- `user_avatar` text nullable
- `rating` smallint check (`rating` between 1 and 5)
- `title` text nullable
- `comment` text
- `images` jsonb default `'[]'::jsonb`     // масив URL
- `pros` jsonb default `'[]'::jsonb`
- `cons` jsonb default `'[]'::jsonb`
- `is_verified_purchase` bool default `false`
- `order_id` bigint nullable FK -> `orders.id`
- `helpful` jsonb default `'{"count":0,"userIds":[]}'::jsonb`
- `replies` jsonb default `'[]'::jsonb`
- `status` smallint/enum (`pending|approved|rejected`)
- `moderated_by_id` uuid nullable FK -> `AspNetUsers.id`
- `moderated_at` timestamptz nullable
- `created_at`, `updated_at` timestamptz

Індекси:
- (`product_id`, `status`, `created_at` desc)
- (`user_id`)
- (`rating`)
- `gin (images jsonb_path_ops)`
- `gin (helpful jsonb_path_ops)`
- `gin (replies jsonb_path_ops)`

---

## 11. Обране (було Mongo)

### 🟦 `favorites`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigserial PK
- `user_id` uuid FK -> `AspNetUsers.id`
- `product_id` bigint FK -> `products.id`
- `added_at` timestamptz
- `price_at_add` numeric(14,2) nullable
- `is_available` bool default `true`
- `notifications` jsonb default `'{"priceDrop":true,"backInStock":true}'::jsonb`
- `meta` json nullable                      // додаткові неіндексовані дані
- `created_at`, `updated_at` timestamptz

Обмеження та індекси:
- unique (`user_id`, `product_id`)
- (`user_id`, `added_at` desc)
- (`product_id`)
- `gin (notifications jsonb_path_ops)`

---

## 12. Поведінка користувача (було Mongo)

### 🟦 `product_views`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigserial PK
- `user_id` uuid nullable FK -> `AspNetUsers.id`
- `product_id` bigint FK -> `products.id`
- `session_id` text
- `viewed_at` timestamptz
- `time_spent` int nullable                 // секунди
- `source` smallint/enum (`search|category|recommendation|direct`)
- `device_type` smallint/enum (`mobile|desktop`)
- `metadata` jsonb                          // пошуковий запит, categoryId, referrer
- `raw_context` json nullable
- `created_at`, `updated_at` timestamptz
- retention: 180 днів (партиції + cleanup)

Індекси:
- (`user_id`, `viewed_at` desc)
- (`product_id`)
- (`session_id`)
- `gin (metadata jsonb_path_ops)`

### 🟦 `search_history`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigserial PK
- `user_id` uuid nullable FK -> `AspNetUsers.id`
- `session_id` text
- `query` text
- `filters` jsonb
- `results_count` int
- `clicked_product_ids` jsonb default `'[]'::jsonb`
- `searched_at` timestamptz
- `raw_payload` json nullable
- `created_at`, `updated_at` timestamptz
- retention: 90 днів (партиції + cleanup)

Індекси:
- (`user_id`, `searched_at` desc)
- `gin (filters jsonb_path_ops)`
- `gin (to_tsvector('simple', query))`

### 🟦 `user_behavior_daily`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigserial PK
- `user_id` uuid FK -> `AspNetUsers.id`
- `date` date
- `metrics` jsonb                           // views, searches, cartAdds, purchases...
- `preferences` jsonb                       // favoriteCategories, brands, priceRange...
- `notes` json nullable
- `created_at`, `updated_at` timestamptz
- retention: 365 днів

Обмеження та індекси:
- unique (`user_id`, `date`)
- (`date`)
- `gin (metrics jsonb_path_ops)`
- `gin (preferences jsonb_path_ops)`

### 🟥 Redis
- `user:search:suggestions:{prefix}` -> list, TTL 1 година
- `recent:searches:global` -> list, TTL 24 години

---

## 13. Чати та повідомлення (було Mongo)

### 🟦 `chats`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` uuid PK
- `type` smallint/enum (`direct|support|order_related`)
- `order_id` bigint nullable
- `product_id` bigint nullable
- `last_message_text` text nullable
- `last_message_sender_id` uuid nullable FK -> `AspNetUsers.id`
- `last_message_created_at` timestamptz nullable
- `is_active` bool
- `meta` jsonb
- `participants_snapshot` jsonb nullable    // для сумісності зі старим документним форматом
- `raw_payload` json nullable
- `created_at`, `updated_at` timestamptz

Індекси:
- (`order_id`)
- (`product_id`)
- `gin (meta jsonb_path_ops)`
- `gin (participants_snapshot jsonb_path_ops)`

### 🟦 `chat_participants`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `chat_id` uuid FK
- `user_id` uuid FK -> `AspNetUsers.id`
- `role` smallint/enum (`buyer|seller|support`)
- `company_id` bigint nullable
- `joined_at` timestamptz
- `left_at` timestamptz nullable
- PK (`chat_id`, `user_id`)

### 🟦 `messages`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigserial PK
- `chat_id` uuid FK
- `sender_id` uuid FK -> `AspNetUsers.id`
- `text` text
- `attachments` jsonb
- `is_read` bool
- `read_at` timestamptz nullable
- `deleted_by` jsonb
- `reply_to_message_id` bigint nullable FK -> `messages.id`
- `raw_payload` json nullable
- `created_at` timestamptz

Індекси:
- `messages(chat_id, created_at desc)`
- `messages(sender_id)`
- `gin (attachments jsonb_path_ops)`
- `gin (deleted_by jsonb_path_ops)`

### 🟥 Redis
- `chat:typing:{chatId}` -> hash, TTL 30 сек

---

## 14. Сповіщення (було Mongo)

### 🟦 `notifications`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigserial PK
- `user_id` uuid FK -> `AspNetUsers.id`
- `type` smallint/enum (`order|payment|review|system|promo`)
- `title` text
- `message` text
- `data` jsonb
- `action_url` text nullable
- `is_read` bool
- `read_at` timestamptz nullable
- `expires_at` timestamptz nullable
- `raw_payload` json nullable
- `created_at` timestamptz

Індекси:
- (`user_id`, `is_read`, `created_at` desc)
- (`expires_at`)
- `gin (data jsonb_path_ops)`

### 🟥 Redis
- `user:notifications:count:{userId}` -> string, TTL 1 година

---

## 15. Скарги

### 🟦 `reports`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigint PK
- `reporter_user_id` text FK
- `target_type` smallint/enum (`product|company|review|message|user|order`)
- `target_id` text
- `reason` smallint/enum (`fraud|inappropriate|spam|counterfeit|other`)
- `description` text
- `images` jsonb
- `status` smallint/enum (`pending|reviewed|resolved|rejected`)
- `reviewed_by_id` text nullable FK
- `reviewed_at` timestamptz nullable
- `resolution` text nullable
- `priority` smallint/enum (`low|normal|high|critical`)
- `created_at`, `updated_at` timestamptz

---

## 16. Підтримка

### 🟦 `support_tickets`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigint PK
- `ticket_number` text unique
- `user_id` text FK
- `order_id` bigint nullable FK
- `subject` text
- `message` text
- `status` smallint/enum (`open|in_progress|waiting_customer|resolved|closed`)
- `priority` smallint/enum (`low|normal|high|urgent`)
- `category_id` bigint nullable
- `assigned_to_id` text nullable FK
- `last_message_at` timestamptz nullable
- `resolved_at` timestamptz nullable
- `closed_at` timestamptz nullable
- `created_at`, `updated_at` timestamptz

### 🟦 `support_ticket_messages`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigint PK
- `ticket_id` bigint FK
- `sender_id` text FK
- `message` text
- `attachments` jsonb nullable
- `is_internal` bool
- `created_at` timestamptz

---

## 17. Аналітика та логи

### 🟦 `audit_logs`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigserial PK
- `user_id` text nullable FK
- `action` text
- `entity_type` text
- `entity_id` text
- `old_data` jsonb
- `new_data` jsonb
- `ip_address` text
- `user_agent` text
- `created_at` timestamptz

### 🟦 `activity_logs`
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigserial PK
- `user_id` text nullable FK
- `event_type` smallint/enum (`login|logout|purchase|view|search`)
- `event_data` jsonb
- `ip_address` text
- `created_at` timestamptz

### 🟦 `analytics_events` (було Mongo)
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigserial PK
- `user_id` uuid nullable FK -> `AspNetUsers.id`
- `session_id` text
- `event_type` text
- `event_data` jsonb
- `device_type`, `browser`, `os`, `ip_address` text
- `context_raw` json nullable
- `created_at` timestamptz

Retention: 365 днів (рекомендовано партиціювання).

Індекси:
- (`event_type`, `created_at` desc)
- (`user_id`, `created_at` desc)
- (`session_id`)
- `gin (event_data jsonb_path_ops)`

---

## 18. Безпека

### 🟦 `refresh_tokens` (узгоджено з кодом)
- Soft delete: `deleted_at` timestamptz nullable, `is_deleted` bool default `false`
- `id` bigserial PK
- `user_id` uuid FK -> `AspNetUsers.id`
- `token_hash` varchar(64) unique
- `expires_at` timestamptz
- `revoked_at` timestamptz nullable
- `replaced_by_token_hash` varchar(64) nullable
- `created_at` timestamptz

Облік невдалих спроб входу та блокування — через **ASP.NET Identity** (`AccessFailedCount`, `LockoutEnd`, `LockoutEnabled` на `AspNetUsers` тощо); окрема таблиця `login_attempts` не використовується.

### 🟥 Redis
- `rate:limit:{userId}:{action}` -> counter, TTL 60 сек

---

## Зведений розподіл даних

| Сутність | PostgreSQL | Redis | S3 |
|----------|------------|-------|----|
| Користувачі | ✅ | ✅ | (аватар) |
| Ролі/Claims | ✅ |  |  |
| Компанії | ✅ | ✅ | (лого) |
| Категорії | ✅ | ✅ | (іконки) |
| Товари | ✅ | ✅ | (фото) |
| Кошик | ✅ | ✅ |  |
| Замовлення | ✅ | ✅ |  |
| Платежі | ✅ | ✅ |  |
| Купони | ✅ | ✅ |  |
| Відгуки товарів/компаній | ✅ | ✅ (rating cache) | (фото) |
| Обране | ✅ |  |  |
| Перегляди/пошук/поведінка | ✅ | ✅ |  |
| Чати/повідомлення | ✅ | ✅ | (файли) |
| Сповіщення | ✅ | ✅ |  |
| Скарги | ✅ |  | (фото) |
| Підтримка | ✅ |  | (файли) |
| Аудит/аналітика | ✅ |  |  |

---

## Синхронізація між системами (оновлено)

- **PostgreSQL -> Redis**
  - інвалідація та прогрів кешів продуктів/категорій/замовлень;
  - онлайн-стани, тайпінг, unread counters.
- **Redis -> PostgreSQL**
  - батч-злив лічильників (`views`, `trending`, інші counter-и);
  - cart finalize при checkout.
- **S3 <-> PostgreSQL**
  - у БД зберігаються URL і метадані файлів;
  - доступ до файлів через CDN.

---

## Технічні рекомендації

- Для high-volume таблиць (`analytics_events`, `product_views`, `search_history`, `messages`) використовувати **partition by range(created_at)**.
- Для `jsonb`:
  - `GIN` індекси (`jsonb_path_ops`) на поля з пошуком/фільтрацією;
  - часті фільтри виносити в окремі колонки.
- Для історичних даних замовлення — використовувати snapshot таблиці (`order_addresses`, `order_items`).
- Для рейтингів/лічильників — materialized views або scheduled aggregation jobs.


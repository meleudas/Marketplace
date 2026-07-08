-- Test data seed for Marketplace (PostgreSQL)
-- Run inside docker via db-seed service.
--
-- Known test password for ALL seeded users:
--   Admin123!
-- E2E helpers:
--   unverified@marketplace.test  (EmailConfirmed=false)
--   twofa@marketplace.test       (TwoFactorEnabled=true)
-- ASP.NET Identity hash (PasswordHasher v3 / Identity):
--   AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==
--
-- Includes: Identity + marketplace_users, categories, companies (uuid), members, contracts,
-- products (active + pending moderation), inventory, carts + cart_stock_watches,
-- orders + status history + payments/refunds, reviews,
-- P4 multi-warehouse (allocations, split stock, order 4), shipments,
-- finance (order_financials, seller_ledger, settlement_batches, seller_payouts, payout IBAN),
-- coupons, returns, chats,
-- notifications (in-app), push_subscriptions, outbox/inbox samples.

\set ON_ERROR_STOP on

BEGIN;

-- 1) Clean dependent data (order: children first; skip tables missing on older schemas)
DO $$
BEGIN
    IF to_regclass('public.notifications') IS NOT NULL THEN DELETE FROM notifications; END IF;
    IF to_regclass('public.push_subscriptions') IS NOT NULL THEN DELETE FROM push_subscriptions; END IF;
    IF to_regclass('public.cart_stock_watches') IS NOT NULL THEN DELETE FROM cart_stock_watches; END IF;
    IF to_regclass('public.http_idempotency_requests') IS NOT NULL THEN DELETE FROM http_idempotency_requests; END IF;
    IF to_regclass('public.inbox_messages') IS NOT NULL THEN DELETE FROM inbox_messages; END IF;
    IF to_regclass('public.outbox_messages') IS NOT NULL THEN DELETE FROM outbox_messages; END IF;
    IF to_regclass('public.order_status_history') IS NOT NULL THEN DELETE FROM order_status_history; END IF;
    IF to_regclass('public.inventory_reservations') IS NOT NULL THEN DELETE FROM inventory_reservations; END IF;
    IF to_regclass('public.review_replies') IS NOT NULL THEN DELETE FROM review_replies; END IF;
    IF to_regclass('public.company_reviews') IS NOT NULL THEN DELETE FROM company_reviews; END IF;
    IF to_regclass('public.product_reviews') IS NOT NULL THEN DELETE FROM product_reviews; END IF;
    IF to_regclass('public.refunds') IS NOT NULL THEN DELETE FROM refunds; END IF;
    IF to_regclass('public.seller_payouts') IS NOT NULL THEN DELETE FROM seller_payouts; END IF;
    IF to_regclass('public.seller_ledger_entries') IS NOT NULL THEN DELETE FROM seller_ledger_entries; END IF;
    IF to_regclass('public.settlement_batches') IS NOT NULL THEN DELETE FROM settlement_batches; END IF;
    IF to_regclass('public.order_financials') IS NOT NULL THEN DELETE FROM order_financials; END IF;
    IF to_regclass('public.shipping_events') IS NOT NULL THEN DELETE FROM shipping_events; END IF;
    IF to_regclass('public.shipment_items') IS NOT NULL THEN DELETE FROM shipment_items; END IF;
    IF to_regclass('public.shipments') IS NOT NULL THEN DELETE FROM shipments; END IF;
    IF to_regclass('public.order_fulfillment_allocations') IS NOT NULL THEN DELETE FROM order_fulfillment_allocations; END IF;
    IF to_regclass('public.return_line_items') IS NOT NULL THEN DELETE FROM return_line_items; END IF;
    IF to_regclass('public.return_requests') IS NOT NULL THEN DELETE FROM return_requests; END IF;
    IF to_regclass('public.chat_messages') IS NOT NULL THEN DELETE FROM chat_messages; END IF;
    IF to_regclass('public.chat_read_states') IS NOT NULL THEN DELETE FROM chat_read_states; END IF;
    IF to_regclass('public.chat_participants') IS NOT NULL THEN DELETE FROM chat_participants; END IF;
    IF to_regclass('public.chats') IS NOT NULL THEN DELETE FROM chats; END IF;
    IF to_regclass('public.coupon_usages') IS NOT NULL THEN DELETE FROM coupon_usages; END IF;
    IF to_regclass('public.coupons') IS NOT NULL THEN DELETE FROM coupons; END IF;
    IF to_regclass('public.payments') IS NOT NULL THEN DELETE FROM payments; END IF;
    IF to_regclass('public.order_addresses') IS NOT NULL THEN DELETE FROM order_addresses; END IF;
    IF to_regclass('public.order_items') IS NOT NULL THEN DELETE FROM order_items; END IF;
    IF to_regclass('public.orders') IS NOT NULL THEN DELETE FROM orders; END IF;
    IF to_regclass('public.favorites') IS NOT NULL THEN DELETE FROM favorites; END IF;
    IF to_regclass('public.cart_items') IS NOT NULL THEN DELETE FROM cart_items; END IF;
    IF to_regclass('public.carts') IS NOT NULL THEN DELETE FROM carts; END IF;
    IF to_regclass('public.stock_movements') IS NOT NULL THEN DELETE FROM stock_movements; END IF;
    IF to_regclass('public.warehouse_stocks') IS NOT NULL THEN DELETE FROM warehouse_stocks; END IF;
    IF to_regclass('public.warehouses') IS NOT NULL THEN DELETE FROM warehouses; END IF;
    IF to_regclass('public.product_images') IS NOT NULL THEN DELETE FROM product_images; END IF;
    IF to_regclass('public.product_details') IS NOT NULL THEN DELETE FROM product_details; END IF;
    IF to_regclass('public.products') IS NOT NULL THEN DELETE FROM products; END IF;
    IF to_regclass('public.company_commission_rates') IS NOT NULL THEN DELETE FROM company_commission_rates; END IF;
    IF to_regclass('public.company_contracts') IS NOT NULL THEN DELETE FROM company_contracts; END IF;
    IF to_regclass('public.company_legal_profiles') IS NOT NULL THEN DELETE FROM company_legal_profiles; END IF;
    IF to_regclass('public.company_members') IS NOT NULL THEN DELETE FROM company_members; END IF;
END $$;

DELETE FROM refresh_tokens;
DELETE FROM marketplace_users;
DELETE FROM companies;
DELETE FROM categories;
DELETE FROM "AspNetUserRoles";
DELETE FROM "AspNetRoles";
DELETE FROM "AspNetUsers";

-- 2) Reset categories identity sequence (if exists)
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_class WHERE relname = 'categories_Id_seq') THEN
        EXECUTE 'ALTER SEQUENCE "categories_Id_seq" RESTART WITH 1';
    END IF;
END $$;

-- 3) Seed Identity users (same PasswordHash = Admin123!)
INSERT INTO "AspNetUsers" (
    "Id",
    "AccessFailedCount",
    "ConcurrencyStamp",
    "Email",
    "EmailConfirmed",
    "IsDeleted",
    "LockoutEnabled",
    "LockoutEnd",
    "NormalizedEmail",
    "NormalizedUserName",
    "PasswordHash",
    "PhoneNumber",
    "PhoneNumberConfirmed",
    "SecurityStamp",
    "TelegramChatId",
    "TelegramLinkedAtUtc",
    "TelegramTwoFactorEnabled",
    "TwoFactorEnabled",
    "UserName"
) VALUES
(
    '11111111-1111-1111-1111-111111111111',
    0,
    'seed-admin-concurrency',
    'admin@marketplace.test',
    TRUE,
    FALSE,
    FALSE,
    NULL,
    'ADMIN@MARKETPLACE.TEST',
    'ADMIN',
    'AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==',
    '+380000000001',
    FALSE,
    'seed-admin-security',
    NULL,
    NULL,
    FALSE,
    FALSE,
    'admin'
),
(
    '22222222-2222-2222-2222-222222222222',
    0,
    'seed-seller-concurrency',
    'seller@marketplace.test',
    TRUE,
    FALSE,
    FALSE,
    NULL,
    'SELLER@MARKETPLACE.TEST',
    'SELLER',
    'AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==',
    '+380000000002',
    FALSE,
    'seed-seller-security',
    NULL,
    NULL,
    FALSE,
    FALSE,
    'seller'
),
(
    '33333333-3333-3333-3333-333333333333',
    0,
    'seed-buyer-concurrency',
    'buyer@marketplace.test',
    TRUE,
    FALSE,
    FALSE,
    NULL,
    'BUYER@MARKETPLACE.TEST',
    'BUYER',
    'AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==',
    '+380000000003',
    FALSE,
    'seed-buyer-security',
    NULL,
    NULL,
    FALSE,
    FALSE,
    'buyer'
),
(
    '44444444-4444-4444-4444-444444444444',
    0,
    'seed-mod-concurrency',
    'moderator@marketplace.test',
    TRUE,
    FALSE,
    FALSE,
    NULL,
    'MODERATOR@MARKETPLACE.TEST',
    'MODERATOR',
    'AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==',
    '+380000000004',
    FALSE,
    'seed-mod-security',
    NULL,
    NULL,
    FALSE,
    FALSE,
    'moderator'
),
(
    '55555555-5555-5555-5555-555555555555',
    0,
    'seed-plainuser-concurrency',
    'user@marketplace.test',
    TRUE,
    FALSE,
    FALSE,
    NULL,
    'USER@MARKETPLACE.TEST',
    'PLAINUSER',
    'AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==',
    '+380000000005',
    FALSE,
    'seed-plainuser-security',
    NULL,
    NULL,
    FALSE,
    FALSE,
    'plainuser'
),
(
    '66666666-6666-6666-6666-666666666666',
    0,
    'seed-manager-concurrency',
    'manager@marketplace.test',
    TRUE,
    FALSE,
    FALSE,
    NULL,
    'MANAGER@MARKETPLACE.TEST',
    'MANAGER',
    'AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==',
    '+380000000006',
    FALSE,
    'seed-manager-security',
    NULL,
    NULL,
    FALSE,
    FALSE,
    'manager'
),
(
    '77777777-7777-7777-7777-777777777777',
    0,
    'seed-seller2-concurrency',
    'seller2@marketplace.test',
    TRUE,
    FALSE,
    FALSE,
    NULL,
    'SELLER2@MARKETPLACE.TEST',
    'SELLER2',
    'AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==',
    '+380000000007',
    FALSE,
    'seed-seller2-security',
    NULL,
    NULL,
    FALSE,
    FALSE,
    'seller2'
),
(
    '88888888-8888-8888-8888-888888888888',
    0,
    'seed-support-concurrency',
    'support@marketplace.test',
    TRUE,
    FALSE,
    FALSE,
    NULL,
    'SUPPORT@MARKETPLACE.TEST',
    'SUPPORT',
    'AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==',
    '+380000000008',
    FALSE,
    'seed-support-security',
    NULL,
    NULL,
    FALSE,
    FALSE,
    'support'
),
(
    '99999999-9999-9999-9999-999999999999',
    0,
    'seed-logistics-concurrency',
    'logistics@marketplace.test',
    TRUE,
    FALSE,
    FALSE,
    NULL,
    'LOGISTICS@MARKETPLACE.TEST',
    'LOGISTICS',
    'AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==',
    '+380000000009',
    FALSE,
    'seed-logistics-security',
    NULL,
    NULL,
    FALSE,
    FALSE,
    'logistics'
),
(
    'b0000001-0000-4000-8000-000000000001',
    0,
    'seed-unverified-concurrency',
    'unverified@marketplace.test',
    FALSE,
    FALSE,
    FALSE,
    NULL,
    'UNVERIFIED@MARKETPLACE.TEST',
    'UNVERIFIED',
    'AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==',
    '+380000000010',
    FALSE,
    'seed-unverified-security',
    NULL,
    NULL,
    FALSE,
    FALSE,
    'unverified'
),
(
    'b0000002-0000-4000-8000-000000000002',
    0,
    'seed-twofa-concurrency',
    'twofa@marketplace.test',
    TRUE,
    FALSE,
    FALSE,
    NULL,
    'TWOFA@MARKETPLACE.TEST',
    'TWOFA',
    'AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==',
    '+380000000011',
    FALSE,
    'seed-twofa-security',
    NULL,
    NULL,
    FALSE,
    TRUE,
    'twofa'
);

-- 3.0) App notification preferences + Telegram (columns from AddAppNotificationUserPreferences)
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
         WHERE table_schema = 'public' AND table_name = 'AspNetUsers' AND column_name = 'NotifyAppByEmail'
    ) THEN
        UPDATE "AspNetUsers"
           SET "NotifyAppByEmail" = TRUE,
               "NotifyAppByTelegram" = TRUE;
        UPDATE "AspNetUsers"
           SET "TelegramChatId" = '900000001', "TelegramLinkedAtUtc" = NOW()
         WHERE "Id" = '11111111-1111-1111-1111-111111111111';
        UPDATE "AspNetUsers"
           SET "TelegramChatId" = '900000002', "TelegramLinkedAtUtc" = NOW()
         WHERE "Id" = '22222222-2222-2222-2222-222222222222';
        UPDATE "AspNetUsers"
           SET "TelegramChatId" = '900000004', "TelegramLinkedAtUtc" = NOW()
         WHERE "Id" = '44444444-4444-4444-4444-444444444444';
    END IF;
END $$;

-- 3.1) Seed Identity roles
INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
VALUES
('aaaaaaaa-0000-0000-0000-000000000001', 'Admin', 'ADMIN', 'seed-role-admin'),
('aaaaaaaa-0000-0000-0000-000000000002', 'Seller', 'SELLER', 'seed-role-seller'),
('aaaaaaaa-0000-0000-0000-000000000003', 'Buyer', 'BUYER', 'seed-role-buyer'),
('aaaaaaaa-0000-0000-0000-000000000004', 'Moderator', 'MODERATOR', 'seed-role-moderator'),
('aaaaaaaa-0000-0000-0000-000000000005', 'User', 'USER', 'seed-role-user');

-- 3.2) Global role mapping (JWT / [Authorize(Roles=...)])
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
VALUES
('11111111-1111-1111-1111-111111111111', 'aaaaaaaa-0000-0000-0000-000000000001'),
('22222222-2222-2222-2222-222222222222', 'aaaaaaaa-0000-0000-0000-000000000002'),
('33333333-3333-3333-3333-333333333333', 'aaaaaaaa-0000-0000-0000-000000000003'),
('44444444-4444-4444-4444-444444444444', 'aaaaaaaa-0000-0000-0000-000000000004'),
('55555555-5555-5555-5555-555555555555', 'aaaaaaaa-0000-0000-0000-000000000005'),
('66666666-6666-6666-6666-666666666666', 'aaaaaaaa-0000-0000-0000-000000000002'),
('77777777-7777-7777-7777-777777777777', 'aaaaaaaa-0000-0000-0000-000000000002'),
('88888888-8888-8888-8888-888888888888', 'aaaaaaaa-0000-0000-0000-000000000003'),
('99999999-9999-9999-9999-999999999999', 'aaaaaaaa-0000-0000-0000-000000000003'),
('b0000001-0000-4000-8000-000000000001', 'aaaaaaaa-0000-0000-0000-000000000003'),
('b0000002-0000-4000-8000-000000000002', 'aaaaaaaa-0000-0000-0000-000000000003');

-- 4) marketplace_users (Role: Buyer=1, Seller=2, Moderator=3, Admin=4)
INSERT INTO marketplace_users (
    "Id",
    "FirstName",
    "LastName",
    "Role",
    "Birthday",
    "Avatar",
    "IsVerified",
    "VerificationDocument",
    "LastLoginAt",
    "CreatedAt",
    "UpdatedAt",
    "IsDeleted",
    "DeletedAt"
) VALUES
('11111111-1111-1111-1111-111111111111', 'Admin', 'User', 4, NULL, NULL, TRUE, NULL, NOW(), NOW(), NOW(), FALSE, NULL),
('22222222-2222-2222-2222-222222222222', 'Seller', 'One', 2, NULL, NULL, TRUE, NULL, NOW(), NOW(), NOW(), FALSE, NULL),
('33333333-3333-3333-3333-333333333333', 'Buyer', 'One', 1, NULL, NULL, TRUE, NULL, NOW(), NOW(), NOW(), FALSE, NULL),
('44444444-4444-4444-4444-444444444444', 'Mod', 'Erator', 3, NULL, NULL, TRUE, NULL, NOW(), NOW(), NOW(), FALSE, NULL),
('55555555-5555-5555-5555-555555555555', 'Plain', 'User', 1, NULL, NULL, TRUE, NULL, NOW(), NOW(), NOW(), FALSE, NULL),
('66666666-6666-6666-6666-666666666666', 'Company', 'Manager', 2, NULL, NULL, TRUE, NULL, NOW(), NOW(), NOW(), FALSE, NULL),
('77777777-7777-7777-7777-777777777777', 'Company', 'Seller', 2, NULL, NULL, TRUE, NULL, NOW(), NOW(), NOW(), FALSE, NULL),
('88888888-8888-8888-8888-888888888888', 'Support', 'Agent', 1, NULL, NULL, TRUE, NULL, NOW(), NOW(), NOW(), FALSE, NULL),
('99999999-9999-9999-9999-999999999999', 'Logistics', 'Lead', 1, NULL, NULL, TRUE, NULL, NOW(), NOW(), NOW(), FALSE, NULL),
('b0000001-0000-4000-8000-000000000001', 'Unverified', 'User', 1, NULL, NULL, FALSE, NULL, NOW(), NOW(), NOW(), FALSE, NULL),
('b0000002-0000-4000-8000-000000000002', 'TwoFactor', 'User', 1, NULL, NULL, TRUE, NULL, NOW(), NOW(), NOW(), FALSE, NULL);

-- 4.1) Refresh tokens
INSERT INTO refresh_tokens ("Id", "UserId", "TokenHash", "ExpiresAt", "CreatedAt", "RevokedAt", "ReplacedByTokenHash")
VALUES
(
    'aaaaaaaa-1111-1111-1111-111111111111',
    '33333333-3333-3333-3333-333333333333',
    '1111111111111111111111111111111111111111111111111111111111111111',
    NOW() + INTERVAL '30 days',
    NOW(),
    NULL,
    NULL
),
(
    'bbbbbbbb-2222-2222-2222-222222222222',
    '22222222-2222-2222-2222-222222222222',
    '2222222222222222222222222222222222222222222222222222222222222222',
    NOW() + INTERVAL '30 days',
    NOW(),
    NULL,
    NULL
);

-- 5) Categories (bookstore hierarchy: 4 roots + nested subcategories)
-- Canonical mirror: backend/src/Marketplace.Infrastructure/Persistence/Seeders/BookCatalogCategorySeedData.cs
INSERT INTO categories (
    "Id",
    "Name",
    "Slug",
    "ImageUrl",
    "ParentId",
    "Description",
    "MetaRaw",
    "SortOrder",
    "IsActive",
    "CreatedAt",
    "UpdatedAt",
    "IsDeleted",
    "DeletedAt"
) VALUES
(1, 'Художня література', 'fiction', NULL, NULL, 'Романи, детективи, фантастика та класика', '{"seed":true,"segment":"books","level":"root"}', 1, TRUE, NOW(), NOW(), FALSE, NULL),
(11, 'Романи', 'fiction-novels', NULL, 1, 'Художні романи', '{"seed":true,"segment":"books","parent":"fiction"}', 1, TRUE, NOW(), NOW(), FALSE, NULL),
(12, 'Детективи', 'fiction-detectives', NULL, 1, 'Детективні романи', '{"seed":true,"segment":"books","parent":"fiction"}', 2, TRUE, NOW(), NOW(), FALSE, NULL),
(13, 'Фентезі', 'fiction-fantasy', NULL, 1, 'Фентезійні романи', '{"seed":true,"segment":"books","parent":"fiction"}', 3, TRUE, NOW(), NOW(), FALSE, NULL),
(14, 'Класика', 'fiction-classics', NULL, 1, 'Класична художня література', '{"seed":true,"segment":"books","parent":"fiction"}', 4, TRUE, NOW(), NOW(), FALSE, NULL),
(15, 'Трилери', 'fiction-thrillers', NULL, 1, 'Психологічні та саспенс трилери', '{"seed":true,"segment":"books","parent":"fiction"}', 5, TRUE, NOW(), NOW(), FALSE, NULL),
(16, 'Проза', 'fiction-prose', NULL, 1, 'Сучасна та класична проза', '{"seed":true,"segment":"books","parent":"fiction"}', 6, TRUE, NOW(), NOW(), FALSE, NULL),
(17, 'Фантастика', 'fiction-sci-fi', NULL, 1, 'Наукова фантастика', '{"seed":true,"segment":"books","parent":"fiction"}', 7, TRUE, NOW(), NOW(), FALSE, NULL),
(18, 'Містика і жахи', 'fiction-mystery-horror', NULL, 1, 'Містика, horror та dark fiction', '{"seed":true,"segment":"books","parent":"fiction"}', 8, TRUE, NOW(), NOW(), FALSE, NULL),
(19, 'Пригоди', 'fiction-adventure', NULL, 1, 'Пригодницькі романи', '{"seed":true,"segment":"books","parent":"fiction"}', 9, TRUE, NOW(), NOW(), FALSE, NULL),
(20, 'Колекційні і лімітовані видання', 'fiction-collectible', NULL, 1, 'Колекційні та лімітовані видання', '{"seed":true,"segment":"books","parent":"fiction"}', 10, TRUE, NOW(), NOW(), FALSE, NULL),

(2, 'Документальна література', 'documentary', NULL, NULL, 'Біографії, історія, наука та нон-фікшн', '{"seed":true,"segment":"books","level":"root"}', 2, TRUE, NOW(), NOW(), FALSE, NULL),
(21, 'Біографії та мемуари', 'documentary-biographies', NULL, 2, 'Біографії, автобіографії та мемуари', '{"seed":true,"segment":"books","parent":"documentary"}', 1, TRUE, NOW(), NOW(), FALSE, NULL),
(22, 'Історія', 'documentary-history', NULL, 2, 'Історична документальна література', '{"seed":true,"segment":"books","parent":"documentary"}', 2, TRUE, NOW(), NOW(), FALSE, NULL),
(23, 'Наука та популяризація', 'documentary-science', NULL, 2, 'Науково-популярні видання', '{"seed":true,"segment":"books","parent":"documentary"}', 3, TRUE, NOW(), NOW(), FALSE, NULL),
(24, 'Психологія та саморозвиток', 'documentary-psychology', NULL, 2, 'Психологія, мотивація, soft skills', '{"seed":true,"segment":"books","parent":"documentary"}', 4, TRUE, NOW(), NOW(), FALSE, NULL),
(25, 'Бізнес та економіка', 'documentary-business', NULL, 2, 'Бізнес, менеджмент, фінанси', '{"seed":true,"segment":"books","parent":"documentary"}', 5, TRUE, NOW(), NOW(), FALSE, NULL),
(26, 'Політика та суспільство', 'documentary-politics', NULL, 2, 'Політика, соціологія, суспільні процеси', '{"seed":true,"segment":"books","parent":"documentary"}', 6, TRUE, NOW(), NOW(), FALSE, NULL),
(27, 'Подорожі', 'documentary-travel', NULL, 2, 'Подорожі, країнознавство, репортажі', '{"seed":true,"segment":"books","parent":"documentary"}', 7, TRUE, NOW(), NOW(), FALSE, NULL),
(28, 'True crime', 'documentary-true-crime', NULL, 2, 'Реальні злочини та розслідування', '{"seed":true,"segment":"books","parent":"documentary"}', 8, TRUE, NOW(), NOW(), FALSE, NULL),

(3, 'Дитяча література', 'children', NULL, NULL, 'Книги для дітей та підлітків', '{"seed":true,"segment":"books","level":"root"}', 3, TRUE, NOW(), NOW(), FALSE, NULL),
(31, 'Казки та малюки', 'children-fairy-tales', NULL, 3, 'Казки, малюки та bedtime stories', '{"seed":true,"segment":"books","parent":"children"}', 1, TRUE, NOW(), NOW(), FALSE, NULL),
(32, 'Для дошкільнят', 'children-preschool', NULL, 3, 'Книги для дошкільного віку', '{"seed":true,"segment":"books","parent":"children"}', 2, TRUE, NOW(), NOW(), FALSE, NULL),
(33, 'Для молодших школярів', 'children-elementary', NULL, 3, 'Книги для молодших школярів', '{"seed":true,"segment":"books","parent":"children"}', 3, TRUE, NOW(), NOW(), FALSE, NULL),
(34, 'Підліткова література', 'children-ya', NULL, 3, 'Young adult та підліткова проза', '{"seed":true,"segment":"books","parent":"children"}', 4, TRUE, NOW(), NOW(), FALSE, NULL),
(35, 'Розмальовки та активності', 'children-activity', NULL, 3, 'Розмальовки, наліпки, активності', '{"seed":true,"segment":"books","parent":"children"}', 5, TRUE, NOW(), NOW(), FALSE, NULL),
(36, 'Навчальні книги для дітей', 'children-educational', NULL, 3, 'Розвиваючі та навчальні книги', '{"seed":true,"segment":"books","parent":"children"}', 6, TRUE, NOW(), NOW(), FALSE, NULL),
(37, 'Комікси для дітей', 'children-comics', NULL, 3, 'Дитячі комікси та graphic novels', '{"seed":true,"segment":"books","parent":"children"}', 7, TRUE, NOW(), NOW(), FALSE, NULL),
(38, 'Книги-м''якотики', 'children-soft-books', NULL, 3, 'М''які книжки для найменших', '{"seed":true,"segment":"books","parent":"children"}', 8, TRUE, NOW(), NOW(), FALSE, NULL),

(4, 'Освітня література', 'education', NULL, NULL, 'Підручники, довідники та навчальні матеріали', '{"seed":true,"segment":"books","level":"root"}', 4, TRUE, NOW(), NOW(), FALSE, NULL),
(41, 'Шкільні підручники', 'education-textbooks', NULL, 4, 'Підручники для школи', '{"seed":true,"segment":"books","parent":"education"}', 1, TRUE, NOW(), NOW(), FALSE, NULL),
(42, 'Іноземні мови', 'education-languages', NULL, 4, 'Підручники та посібники з іноземних мов', '{"seed":true,"segment":"books","parent":"education"}', 2, TRUE, NOW(), NOW(), FALSE, NULL),
(43, 'Математика та точні науки', 'education-stem', NULL, 4, 'Математика, фізика, хімія, біологія', '{"seed":true,"segment":"books","parent":"education"}', 3, TRUE, NOW(), NOW(), FALSE, NULL),
(44, 'Гуманітарні дисципліни', 'education-humanities', NULL, 4, 'Історія, література, філософія для навчання', '{"seed":true,"segment":"books","parent":"education"}', 4, TRUE, NOW(), NOW(), FALSE, NULL),
(45, 'IT та програмування', 'education-it', NULL, 4, 'Підручники з IT та програмування', '{"seed":true,"segment":"books","parent":"education"}', 5, TRUE, NOW(), NOW(), FALSE, NULL),
(46, 'ЗНО/НМТ', 'education-exam-prep', NULL, 4, 'Підготовка до ЗНО та НМТ', '{"seed":true,"segment":"books","parent":"education"}', 6, TRUE, NOW(), NOW(), FALSE, NULL),
(47, 'Методична література', 'education-methodology', NULL, 4, 'Методичні посібники для педагогів', '{"seed":true,"segment":"books","parent":"education"}', 7, TRUE, NOW(), NOW(), FALSE, NULL),
(48, 'Словники та довідники', 'education-dictionaries', NULL, 4, 'Словники, енциклопедії та довідники', '{"seed":true,"segment":"books","parent":"education"}', 8, TRUE, NOW(), NOW(), FALSE, NULL);

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_class WHERE relname = 'categories_Id_seq') THEN
        PERFORM setval('"categories_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM categories), TRUE);
    END IF;
END $$;

-- 6) Companies (legacy bigint + current uuid)
DO $$
DECLARE
    company_id_type text;
    has_contact_name_phone boolean;
BEGIN
    SELECT data_type
      INTO company_id_type
      FROM information_schema.columns
     WHERE table_schema = 'public'
       AND table_name = 'companies'
       AND column_name = 'Id';

    SELECT EXISTS (
        SELECT 1
          FROM information_schema.columns
         WHERE table_schema = 'public'
           AND table_name = 'companies'
           AND column_name = 'AddressFirstName'
    )
      INTO has_contact_name_phone;

    IF company_id_type = 'bigint' THEN
        IF has_contact_name_phone THEN
            EXECUTE $sql$
                INSERT INTO companies (
                    "Id","Name","Slug","Description","ImageUrl",
                    "ContactEmail","ContactPhone",
                    "AddressFirstName","AddressLastName","AddressStreet","AddressCity","AddressState","AddressPostalCode","AddressCountry","AddressPhone",
                    "IsApproved","ApprovedAt","ApprovedByUserId","Rating","MetaRaw","CreatedAt","UpdatedAt","IsDeleted","DeletedAt"
                ) VALUES
                (
                    1,'Книжковий світ','book-world','Книжкова крамниця з художньою та дитячою літературою',NULL,
                    'contact@book-world.test','+380500000001',
                    'Book','World','Main street 1','Kyiv','Kyiv','01001','UA','+380500000001',
                    TRUE,NOW(),'11111111-1111-1111-1111-111111111111',4.7,'{"seed":true,"segment":"books"}',NOW(),NOW(),FALSE,NULL
                ),
                (
                    2,'Література Плюс','literatura-plus','Магазин нон-фікшн та детективів',NULL,
                    'hello@literatura-plus.test','+380500000002',
                    'Literature','Plus','Comfort ave 7','Lviv','Lviv','79000','UA','+380500000002',
                    TRUE,NOW(),'11111111-1111-1111-1111-111111111111',4.2,'{"seed":true,"segment":"books"}',NOW(),NOW(),FALSE,NULL
                );
            $sql$;
        ELSE
            EXECUTE $sql$
                INSERT INTO companies (
                    "Id","Name","Slug","Description","ImageUrl",
                    "ContactEmail","ContactPhone",
                    "AddressStreet","AddressCity","AddressState","AddressPostalCode","AddressCountry",
                    "IsApproved","ApprovedAt","ApprovedByUserId","Rating","MetaRaw","CreatedAt","UpdatedAt","IsDeleted","DeletedAt"
                ) VALUES
                (
                    1,'Книжковий світ','book-world','Книжкова крамниця з художньою та дитячою літературою',NULL,
                    'contact@book-world.test','+380500000001',
                    'Main street 1','Kyiv','Kyiv','01001','UA',
                    TRUE,NOW(),'11111111-1111-1111-1111-111111111111',4.7,'{"seed":true,"segment":"books"}',NOW(),NOW(),FALSE,NULL
                ),
                (
                    2,'Література Плюс','literatura-plus','Магазин нон-фікшн та детективів',NULL,
                    'hello@literatura-plus.test','+380500000002',
                    'Comfort ave 7','Lviv','Lviv','79000','UA',
                    TRUE,NOW(),'11111111-1111-1111-1111-111111111111',4.2,'{"seed":true,"segment":"books"}',NOW(),NOW(),FALSE,NULL
                );
            $sql$;
        END IF;
    ELSE
        EXECUTE $sql$
            INSERT INTO companies (
                "Id","Name","Slug","Description","ImageUrl",
                "ContactEmail","ContactPhone",
                "AddressStreet","AddressCity","AddressState","AddressPostalCode","AddressCountry",
                "IsApproved","ApprovedAt","ApprovedByUserId","Rating","MetaRaw","CreatedAt","UpdatedAt","IsDeleted","DeletedAt"
            ) VALUES
            (
                'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','Книжковий світ','book-world','Книжкова крамниця з художньою та дитячою літературою',NULL,
                'contact@book-world.test','+380500000001',
                'Main street 1','Kyiv','Kyiv','01001','UA',
                TRUE,NOW(),'11111111-1111-1111-1111-111111111111',4.7,'{"seed":true,"segment":"books"}',NOW(),NOW(),FALSE,NULL
            ),
            (
                'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb','Література Плюс','literatura-plus','Магазин нон-фікшн та детективів',NULL,
                'hello@literatura-plus.test','+380500000002',
                'Comfort ave 7','Lviv','Lviv','79000','UA',
                TRUE,NOW(),'11111111-1111-1111-1111-111111111111',4.2,'{"seed":true,"segment":"books"}',NOW(),NOW(),FALSE,NULL
            );
        $sql$;
    END IF;
END $$;

-- 7) Members, catalog, inventory (uuid companies + migrated schema only)
DO $$
DECLARE
    cid_type text;
    has_company_members boolean;
    prod_id int;
    si int;
    j int;
    subcat_ids int[] := ARRAY[
        11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
        21, 22, 23, 24, 25, 26, 27, 28,
        31, 32, 33, 34, 35, 36, 37, 38,
        41, 42, 43, 44, 45, 46, 47, 48
    ];
    subcat_slugs text[] := ARRAY[
        'fiction-novels', 'fiction-detectives', 'fiction-fantasy', 'fiction-classics', 'fiction-thrillers', 'fiction-prose', 'fiction-sci-fi', 'fiction-mystery-horror', 'fiction-adventure', 'fiction-collectible',
        'documentary-biographies', 'documentary-history', 'documentary-science', 'documentary-psychology', 'documentary-business', 'documentary-politics', 'documentary-travel', 'documentary-true-crime',
        'children-fairy-tales', 'children-preschool', 'children-elementary', 'children-ya', 'children-activity', 'children-educational', 'children-comics', 'children-soft-books',
        'education-textbooks', 'education-languages', 'education-stem', 'education-humanities', 'education-it', 'education-exam-prep', 'education-methodology', 'education-dictionaries'
    ];
    subcat_names text[] := ARRAY[
        'Романи', 'Детективи', 'Фентезі', 'Класика', 'Трилери', 'Проза', 'Фантастика', 'Містика і жахи', 'Пригоди', 'Колекційні і лімітовані видання',
        'Біографії та мемуари', 'Історія', 'Наука та популяризація', 'Психологія та саморозвиток', 'Бізнес та економіка', 'Політика та суспільство', 'Подорожі', 'True crime',
        'Казки та малюки', 'Для дошкільнят', 'Для молодших школярів', 'Підліткова література', 'Розмальовки та активності', 'Навчальні книги для дітей', 'Комікси для дітей', 'Книги-м''якотики',
        'Шкільні підручники', 'Іноземні мови', 'Математика та точні науки', 'Гуманітарні дисципліни', 'IT та програмування', 'ЗНО/НМТ', 'Методична література', 'Словники та довідники'
    ];
    book_pool text[] := ARRAY[
        'Кобзар', 'Лісова пісня', 'Тіні забутих предків', 'Intermezzo', 'Земля', 'Майстер і Маргарита', '1984', 'Старий і море',
        'Великий Гетсбі', 'Гордість і упередження', 'Джейн Ейр', 'Війна і мир', 'Анна Кареніна', 'Злочин і кара', 'Ідіот',
        'Брати Карамазови', 'Маленький принц', 'Хобіт', 'Аліса в Країні чудес', 'Пригоди Тома Сойєра', 'Sapiens', 'Atomic Habits',
        'Clean Code', 'Design Patterns', 'Коротка історія майже всього', 'Thinking, Fast and Slow', 'Deep Work', 'Zero to One'
    ];
    author_pool text[] := ARRAY[
        'Тарас Шевченко', 'Леся Українка', 'Михайло Коцюбинський', 'Ольга Кобилянська', 'Михайло Булгаков', 'Джордж Орвелл',
        'Ернест Гемінгуей', 'Ф. Скотт Фіцджеральд', 'Джейн Остін', 'Лев Толстой', 'Федір Достоєвський', 'J.K. Rowling',
        'J.R.R. Tolkien', 'Lewis Carroll', 'Mark Twain', 'Arthur Conan Doyle', 'Agatha Christie', 'Yuval Noah Harari',
        'James Clear', 'Robert Martin', 'Bill Bryson', 'Daniel Kahneman', 'Cal Newport', 'Peter Thiel'
    ];
    book_company uuid;
    book_slug text;
    book_tag text;
    book_image text;
    book_thumb text;
    book_title text;
    book_author text;
    book_price numeric;
    book_category int;
BEGIN
    SELECT c.data_type
      INTO cid_type
      FROM information_schema.columns c
     WHERE c.table_schema = 'public'
       AND c.table_name = 'companies'
       AND c.column_name = 'Id';

    SELECT EXISTS (
        SELECT 1 FROM information_schema.tables
         WHERE table_schema = 'public' AND table_name = 'company_members'
    ) INTO has_company_members;

    IF cid_type = 'uuid' AND has_company_members THEN
    -- Книжковий світ: full role set (CompanyMembershipRole: Owner=0..Logistics=4)
    INSERT INTO company_members ("CompanyId", "UserId", "IsOwner", "Role", "PermissionsRaw", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt")
    VALUES
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '22222222-2222-2222-2222-222222222222', TRUE, 0, NULL, NOW(), NOW(), FALSE, NULL),
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '66666666-6666-6666-6666-666666666666', FALSE, 1, NULL, NOW(), NOW(), FALSE, NULL),
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '77777777-7777-7777-7777-777777777777', FALSE, 2, NULL, NOW(), NOW(), FALSE, NULL),
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '88888888-8888-8888-8888-888888888888', FALSE, 3, NULL, NOW(), NOW(), FALSE, NULL),
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '99999999-9999-9999-9999-999999999999', FALSE, 4, NULL, NOW(), NOW(), FALSE, NULL);

    -- Література Плюс: buyer as owner (second storefront demo)
    INSERT INTO company_members ("CompanyId", "UserId", "IsOwner", "Role", "PermissionsRaw", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt")
    VALUES
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '33333333-3333-3333-3333-333333333333', TRUE, 0, NULL, NOW(), NOW(), FALSE, NULL);

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'products') THEN
        -- Stable anchor products for carts/orders E2E (ids 1..4)
        FOR prod_id IN 1..4 LOOP
            book_company := CASE
                WHEN prod_id = 4 THEN 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'::uuid
                ELSE 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'::uuid
            END;
            book_category := CASE prod_id
                WHEN 1 THEN 14
                WHEN 2 THEN 14
                WHEN 3 THEN 11
                WHEN 4 THEN 16
                ELSE 11
            END;
            book_title := CASE prod_id
                WHEN 1 THEN 'Кобзар'
                WHEN 2 THEN 'Лісова пісня'
                WHEN 3 THEN 'Тіні забутих предків'
                WHEN 4 THEN 'Intermezzo'
                ELSE 'Seed book'
            END;
            book_author := CASE prod_id
                WHEN 1 THEN 'Тарас Шевченко'
                WHEN 2 THEN 'Леся Українка'
                WHEN 3 THEN 'Михайло Коцюбинський'
                WHEN 4 THEN 'Михайло Коцюбинський'
                ELSE 'Seed Author'
            END;
            book_price := CASE prod_id
                WHEN 1 THEN 349
                WHEN 2 THEN 299
                WHEN 3 THEN 279
                WHEN 4 THEN 249
                ELSE 299
            END;
            book_slug := 'seed-book-' || lpad(prod_id::text, 3, '0');
            book_tag := subcat_slugs[array_position(subcat_ids, book_category)];

            INSERT INTO products (
                "Id", "CompanyId", "Name", "Slug", "Description", "Price", "OldPrice", "Stock", "MinStock", "CategoryId", "Status",
                "Rating", "ReviewCount", "ViewCount", "SalesCount", "HasVariants", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
            ) VALUES (
                prod_id,
                book_company,
                book_title,
                book_slug,
                book_author || '. Книга для книжкового маркетплейсу.',
                book_price,
                CASE WHEN prod_id % 3 = 0 THEN book_price + 50 ELSE NULL END,
                0,
                5,
                book_category,
                1,
                round((3.8 + (prod_id % 12) * 0.1)::numeric, 1),
                3 + ((prod_id * 7) % 40),
                50 + prod_id * 17,
                prod_id % 15,
                FALSE,
                NOW(),
                NOW(),
                FALSE,
                NULL
            );

            INSERT INTO product_details (
                "Id", "ProductId", "Slug", "AttributesRaw", "VariantsRaw", "SpecificationsRaw", "SeoRaw", "ContentBlocksRaw", "Tags", "Brands",
                "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
            ) VALUES (
                prod_id,
                prod_id,
                book_slug,
                jsonb_build_object('seed', true, 'author', book_author, 'subcategoryId', book_category),
                '{}'::jsonb,
                '{}'::jsonb,
                '{}'::jsonb,
                '{}'::jsonb,
                ARRAY['seed', 'book', book_tag]::text[],
                ARRAY[book_author]::text[],
                NOW(),
                NOW(),
                FALSE,
                NULL
            );

            INSERT INTO product_images (
                "Id", "ProductId", "ImageUrl", "ThumbnailUrl", "AltText", "SortOrder", "IsMain", "Width", "Height", "FileSize",
                "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
            ) VALUES (
                prod_id,
                prod_id,
                'https://picsum.photos/seed/marketplace-book-' || prod_id || '/400/600',
                'https://picsum.photos/seed/marketplace-book-' || prod_id || '/200/300',
                book_title,
                0,
                TRUE,
                400,
                600,
                NULL,
                NOW(),
                NOW(),
                FALSE,
                NULL
            );
        END LOOP;

        prod_id := 5;
        FOR si IN 1..array_length(subcat_ids, 1) LOOP
            FOR j IN 1..7 LOOP
                book_category := subcat_ids[si];
                book_tag := subcat_slugs[si];
                book_company := CASE
                    WHEN (prod_id + j) % 5 = 0 THEN 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'::uuid
                    ELSE 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'::uuid
                END;
                book_title := subcat_names[si] || ': ' || book_pool[((si - 1 + j - 1) % array_length(book_pool, 1)) + 1];
                book_author := author_pool[((prod_id + si + j - 1) % array_length(author_pool, 1)) + 1];
                book_price := 219 + ((prod_id * 13 + si * 7 + j * 3) % 480);
                book_slug := 'seed-book-' || lpad(prod_id::text, 3, '0');
                book_image := 'https://picsum.photos/seed/marketplace-book-' || prod_id || '/400/600';
                book_thumb := 'https://picsum.photos/seed/marketplace-book-' || prod_id || '/200/300';

                INSERT INTO products (
                    "Id", "CompanyId", "Name", "Slug", "Description", "Price", "OldPrice", "Stock", "MinStock", "CategoryId", "Status",
                    "Rating", "ReviewCount", "ViewCount", "SalesCount", "HasVariants", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
                ) VALUES (
                    prod_id,
                    book_company,
                    book_title,
                    book_slug,
                    book_author || '. Книга з розділу «' || subcat_names[si] || '».',
                    book_price,
                    CASE WHEN prod_id % 4 = 0 THEN book_price + 60 ELSE NULL END,
                    0,
                    5,
                    book_category,
                    1,
                    round((3.7 + ((prod_id + si) % 12) * 0.1)::numeric, 1),
                    2 + ((prod_id * 5 + si) % 35),
                    40 + prod_id * 11,
                    prod_id % 20,
                    FALSE,
                    NOW(),
                    NOW(),
                    FALSE,
                    NULL
                );

                INSERT INTO product_details (
                    "Id", "ProductId", "Slug", "AttributesRaw", "VariantsRaw", "SpecificationsRaw", "SeoRaw", "ContentBlocksRaw", "Tags", "Brands",
                    "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
                ) VALUES (
                    prod_id,
                    prod_id,
                    book_slug,
                    jsonb_build_object('seed', true, 'author', book_author, 'subcategoryId', book_category, 'subcategory', subcat_names[si]),
                    '{}'::jsonb,
                    '{}'::jsonb,
                    '{}'::jsonb,
                    '{}'::jsonb,
                    ARRAY['seed', 'book', book_tag]::text[],
                    ARRAY[book_author]::text[],
                    NOW(),
                    NOW(),
                    FALSE,
                    NULL
                );

                INSERT INTO product_images (
                    "Id", "ProductId", "ImageUrl", "ThumbnailUrl", "AltText", "SortOrder", "IsMain", "Width", "Height", "FileSize",
                    "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
                ) VALUES (
                    prod_id,
                    prod_id,
                    book_image,
                    book_thumb,
                    book_title,
                    0,
                    TRUE,
                    400,
                    600,
                    NULL,
                    NOW(),
                    NOW(),
                    FALSE,
                    NULL
                );

                prod_id := prod_id + 1;
            END LOOP;
        END LOOP;

        IF EXISTS (
            SELECT 1 FROM information_schema.columns
             WHERE table_schema = 'public' AND table_name = 'products' AND column_name = 'SubmittedByUserId'
        ) THEN
            INSERT INTO products (
                "Id", "CompanyId", "Name", "Slug", "Description", "Price", "OldPrice", "Stock", "MinStock", "CategoryId", "Status",
                "Rating", "ReviewCount", "ViewCount", "SalesCount", "HasVariants", "SubmittedByUserId", "ModerationRejectionReason",
                "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
            ) VALUES
            (243, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Книга на модерації', 'seed-book-pending',
             'Рідкісне видання, очікує перевірки модератором.', 899.00, NULL, 0, 2, 12, 3, 0, 0, 0, 0, FALSE,
             '22222222-2222-2222-2222-222222222222', NULL, NOW(), NOW(), FALSE, NULL),
            (244, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Відхилена книга', 'seed-book-rejected',
             'Приклад відхиленого видання.', 749.00, NULL, 0, 2, 14, 0, 0, 0, 0, 0, FALSE,
             '77777777-7777-7777-7777-777777777777', 'Обкладинка не відповідає опису книги.', NOW(), NOW(), FALSE, NULL);

            INSERT INTO product_details (
                "Id", "ProductId", "Slug", "AttributesRaw", "VariantsRaw", "SpecificationsRaw", "SeoRaw", "ContentBlocksRaw", "Tags", "Brands",
                "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
            ) VALUES
            (243, 243, 'seed-book-pending', '{"seed":true,"moderation":"pending"}'::jsonb, '{}'::jsonb, '{}'::jsonb, '{}'::jsonb, '{}'::jsonb,
             ARRAY['book','moderation']::text[], ARRAY['Seed Press']::text[], NOW(), NOW(), FALSE, NULL),
            (244, 244, 'seed-book-rejected', '{"seed":true,"moderation":"rejected"}'::jsonb, '{}'::jsonb, '{}'::jsonb, '{}'::jsonb, '{}'::jsonb,
             ARRAY['book','moderation']::text[], ARRAY['Seed Press']::text[], NOW(), NOW(), FALSE, NULL);

            INSERT INTO product_images (
                "Id", "ProductId", "ImageUrl", "ThumbnailUrl", "AltText", "SortOrder", "IsMain", "Width", "Height", "FileSize",
                "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
            ) VALUES
            (243, 243, 'https://picsum.photos/seed/marketplace-book-pending/400/600', 'https://picsum.photos/seed/marketplace-book-pending/200/300', 'Книга на модерації', 0, TRUE, 400, 600, NULL, NOW(), NOW(), FALSE, NULL),
            (244, 244, 'https://picsum.photos/seed/marketplace-book-rejected/400/600', 'https://picsum.photos/seed/marketplace-book-rejected/200/300', 'Відхилена книга', 0, TRUE, 400, 600, NULL, NOW(), NOW(), FALSE, NULL);
        ELSE
            INSERT INTO products (
                "Id", "CompanyId", "Name", "Slug", "Description", "Price", "OldPrice", "Stock", "MinStock", "CategoryId", "Status",
                "Rating", "ReviewCount", "ViewCount", "SalesCount", "HasVariants", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
            ) VALUES
            (243, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Книга на модерації', 'seed-book-pending',
             'Рідкісне видання, очікує перевірки модератором.', 899.00, NULL, 0, 2, 12, 3, 0, 0, 0, 0, FALSE, NOW(), NOW(), FALSE, NULL),
            (244, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Відхилена книга', 'seed-book-rejected',
             'Приклад відхиленого видання.', 749.00, NULL, 0, 2, 14, 0, 0, 0, 0, 0, FALSE, NOW(), NOW(), FALSE, NULL);

            INSERT INTO product_details (
                "Id", "ProductId", "Slug", "AttributesRaw", "VariantsRaw", "SpecificationsRaw", "SeoRaw", "ContentBlocksRaw", "Tags", "Brands",
                "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
            ) VALUES
            (243, 243, 'seed-book-pending', '{"seed":true,"moderation":"pending"}'::jsonb, '{}'::jsonb, '{}'::jsonb, '{}'::jsonb, '{}'::jsonb,
             ARRAY['book','moderation']::text[], ARRAY['Seed Press']::text[], NOW(), NOW(), FALSE, NULL),
            (244, 244, 'seed-book-rejected', '{"seed":true,"moderation":"rejected"}'::jsonb, '{}'::jsonb, '{}'::jsonb, '{}'::jsonb, '{}'::jsonb,
             ARRAY['book','moderation']::text[], ARRAY['Seed Press']::text[], NOW(), NOW(), FALSE, NULL);

            INSERT INTO product_images (
                "Id", "ProductId", "ImageUrl", "ThumbnailUrl", "AltText", "SortOrder", "IsMain", "Width", "Height", "FileSize",
                "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
            ) VALUES
            (243, 243, 'https://picsum.photos/seed/marketplace-book-pending/400/600', 'https://picsum.photos/seed/marketplace-book-pending/200/300', 'Книга на модерації', 0, TRUE, 400, 600, NULL, NOW(), NOW(), FALSE, NULL),
            (244, 244, 'https://picsum.photos/seed/marketplace-book-rejected/400/600', 'https://picsum.photos/seed/marketplace-book-rejected/200/300', 'Відхилена книга', 0, TRUE, 400, 600, NULL, NOW(), NOW(), FALSE, NULL);
        END IF;

        IF EXISTS (
            SELECT 1 FROM information_schema.columns
             WHERE table_schema = 'public' AND table_name = 'product_images' AND column_name = 'ImageObjectKey'
        ) THEN
            UPDATE product_images SET
                "ImageObjectKey" = 'seed/products/' || "ProductId"::text || '/main.jpg',
                "OriginalObjectKey" = 'seed/products/' || "ProductId"::text || '/original.jpg',
                "ThumbnailObjectKey" = 'seed/products/' || "ProductId"::text || '/thumb.jpg'
             WHERE "ImageObjectKey" = '';
        END IF;

        IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'warehouses') THEN
        INSERT INTO warehouses (
            "Id", "CompanyId", "Name", "Code", "Street", "City", "State", "PostalCode", "Country", "TimeZone", "Priority",
            "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Kyiv Book Warehouse', 'BOOK-KYIV', 'Skladna 1', 'Kyiv', 'Kyiv', '01001', 'UA', 'Europe/Kyiv', 10, TRUE, NOW(), NOW(), FALSE, NULL),
        (2, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Lviv Book Reserve', 'BOOK-LVIV', 'Logistychna 2', 'Lviv', 'Lviv', '79000', 'UA', 'Europe/Kyiv', 20, TRUE, NOW(), NOW(), FALSE, NULL),
        (3, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Literature Plus Lviv', 'LIT-LVIV', 'Comfort 3', 'Lviv', 'Lviv', '79000', 'UA', 'Europe/Kyiv', 10, TRUE, NOW(), NOW(), FALSE, NULL);

        INSERT INTO warehouse_stocks (
            "Id", "CompanyId", "WarehouseId", "ProductId", "OnHand", "Reserved", "ReorderPoint", "Version", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 1, 50, 6, 10, 1, NOW(), NOW(), FALSE, NULL),
        (2, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 2, 0, 0, 3, 1, NOW(), NOW(), FALSE, NULL),
        (3, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 2, 3, 200, 0, 20, 1, NOW(), NOW(), FALSE, NULL),
        (4, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 3, 4, 30, 2, 5, 1, NOW(), NOW(), FALSE, NULL),
        (5, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 2, 1, 30, 1, 10, 1, NOW(), NOW(), FALSE, NULL);

        INSERT INTO stock_movements (
            "Id", "CompanyId", "WarehouseId", "ProductId", "Type", "Quantity", "OperationId", "Reference", "Reason", "ActorUserId",
            "OccurredAt", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 1, 0, 50, 'seed-book-op-001', 'seed', NULL, '99999999-9999-9999-9999-999999999999', NOW(), NOW(), NOW(), FALSE, NULL),
        (2, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 2, 0, 0, 'seed-book-op-002', 'seed-receive-zero', NULL, '99999999-9999-9999-9999-999999999999', NOW(), NOW(), NOW(), FALSE, NULL),
        (3, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 2, 3, 0, 200, 'seed-book-op-003', 'seed', NULL, '99999999-9999-9999-9999-999999999999', NOW(), NOW(), NOW(), FALSE, NULL),
        (4, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 3, 4, 0, 30, 'seed-lit-op-001', 'seed', NULL, '33333333-3333-3333-3333-333333333333', NOW(), NOW(), NOW(), FALSE, NULL);

        INSERT INTO inventory_reservations (
            "Id", "CompanyId", "WarehouseId", "ProductId", "ReservationCode", "Quantity", "Status", "ExpiresAt", "Reference",
            "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 1, 'SEED-RES-DEMO', 5, 0, NOW() + INTERVAL '1 day', 'seed-demo',
         NOW(), NOW(), FALSE, NULL),
        (2, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 1, 'order-4-product-1-wh-1', 1, 2, NOW() + INTERVAL '7 days', 'order-4',
         NOW(), NOW(), FALSE, NULL),
        (3, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 2, 1, 'order-4-product-1-wh-2', 1, 2, NOW() + INTERVAL '7 days', 'order-4',
         NOW(), NOW(), FALSE, NULL);
        END IF;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'company_legal_profiles') THEN
        INSERT INTO company_legal_profiles (
            "Id", "CompanyId", "LegalName", "LegalType", "Edrpou", "Ipn", "CertificateNumber", "IsVatPayer",
            "InitialCommissionPercent", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Книжковий світ LLC', 1, '12345678', '123456789', 'BOOK-CERT-001', TRUE, 7.5000, NOW(), NOW(), FALSE, NULL),
        (2, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Література Плюс LLC', 1, '87654321', '987654321', 'LIT-CERT-001', FALSE, 6.2500, NOW(), NOW(), FALSE, NULL);

        IF EXISTS (
            SELECT 1 FROM information_schema.columns
             WHERE table_schema = 'public' AND table_name = 'company_legal_profiles' AND column_name = 'PayoutIban'
        ) THEN
            UPDATE company_legal_profiles SET
                "PayoutIban" = 'UA213223130000026007233566001',
                "PayoutRecipientName" = 'Книжковий світ LLC'
             WHERE "CompanyId" = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
        END IF;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'company_contracts') THEN
        INSERT INTO company_contracts (
            "Id", "CompanyId", "ContractNumber", "Status", "EffectiveFrom", "EffectiveTo", "SignedAt", "Notes",
            "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'CTR-BOOK-2026-001', 1, NOW() - INTERVAL '30 days', NULL, NOW() - INTERVAL '35 days', 'Seed contract for Книжковий світ',
         NOW(), NOW(), FALSE, NULL),
        (2, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'CTR-LIT-2026-001', 1, NOW() - INTERVAL '20 days', NULL, NOW() - INTERVAL '22 days', 'Seed contract for Література Плюс',
         NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'company_commission_rates') THEN
        INSERT INTO company_commission_rates (
            "Id", "CompanyId", "ContractId", "CommissionPercent", "EffectiveFrom", "EffectiveTo", "Reason", "CreatedByUserId",
            "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 7.5000, NOW() - INTERVAL '30 days', NULL, 'Initial seed commission', '11111111-1111-1111-1111-111111111111',
         NOW(), NOW(), FALSE, NULL),
        (2, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 2, 6.2500, NOW() - INTERVAL '20 days', NULL, 'Initial seed commission', '11111111-1111-1111-1111-111111111111',
         NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'carts') THEN
        INSERT INTO carts ("Id", "UserId", "Status", "LastActivityAt", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt")
        VALUES
        (1, '33333333-3333-3333-3333-333333333333', 0, NOW(), NOW(), NOW(), FALSE, NULL),
        (2, '11111111-1111-1111-1111-111111111111', 0, NOW() - INTERVAL '1 hour', NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'cart_items') THEN
        INSERT INTO cart_items ("Id", "CartId", "ProductId", "Quantity", "PriceAtMoment", "Discount", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt")
        VALUES
        (1, 1, 1, 1, 349.00, 0, NOW(), NOW(), FALSE, NULL),
        (2, 1, 2, 1, 299.00, 0, NOW(), NOW(), FALSE, NULL),
        (3, 1, 3, 2, 279.00, 0, NOW(), NOW(), FALSE, NULL),
        (4, 2, 4, 1, 249.00, 0, NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'cart_stock_watches') THEN
        INSERT INTO cart_stock_watches ("Id", "UserId", "ProductId", "CreatedAtUtc", "LastNotifiedAtUtc")
        VALUES
        (1, '33333333-3333-3333-3333-333333333333', 2, NOW() - INTERVAL '2 days', NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'favorites') THEN
        INSERT INTO favorites ("Id", "UserId", "ProductId", "AddedAt", "PriceAtAdd", "IsAvailable", "NotificationsRaw", "MetaRaw", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt")
        VALUES
        (1, '33333333-3333-3333-3333-333333333333', 2, NOW(), 299.00, FALSE, '{"priceDrop":true,"backInStock":true}', '{"seed":true}', NOW(), NOW(), FALSE, NULL),
        (2, '55555555-5555-5555-5555-555555555555', 4, NOW(), 249.00, TRUE, '{"priceDrop":false,"backInStock":true}', '{"seed":true}', NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'orders') THEN
        INSERT INTO orders (
            "Id", "OrderNumber", "CustomerId", "CompanyId", "Status", "TotalPrice", "Subtotal", "ShippingCost", "DiscountAmount", "TaxAmount",
            "ShippingMethodId", "PaymentMethod", "Notes", "TrackingNumber", "ShippedAt", "DeliveredAt", "CancelledAt", "RefundedAt",
            "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 'ORD-SEED-0001', '11111111-1111-1111-1111-111111111111', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 2, 798.00, 698.00, 100.00, 0.00, 0.00,
         1, 2, 'Admin test order — Processing (2x Кобзар)', NULL, NULL, NULL, NULL, NULL, NOW() - INTERVAL '5 days', NOW(), FALSE, NULL),
        (2, 'ORD-SEED-0002', '33333333-3333-3333-3333-333333333333', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 3, 628.00, 628.00, 0.00, 0.00, 0.00,
         1, 2, 'Buyer order — Shipped (Кобзар + Тіні забутих предків)', 'NP-SEED-0002', NOW() - INTERVAL '1 day', NULL, NULL, NULL, NOW() - INTERVAL '3 days', NOW(), FALSE, NULL),
        (3, 'ORD-SEED-0003', '33333333-3333-3333-3333-333333333333', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 4, 349.00, 249.00, 100.00, 0.00, 0.00,
         1, 2, 'Buyer order from Література Плюс — Delivered (Intermezzo)', 'NP-SEED-0003', NOW() - INTERVAL '7 days', NOW() - INTERVAL '2 days', NULL, NULL, NOW() - INTERVAL '10 days', NOW(), FALSE, NULL),
        (4, 'ORD-SEED-0004', '33333333-3333-3333-3333-333333333333', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 698.00, 698.00, 0.00, 0.00, 0.00,
         1, 2, 'Buyer order — Paid, split fulfillment (2x Кобзар)', NULL, NULL, NULL, NULL, NULL, NOW() - INTERVAL '6 hours', NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'order_items') THEN
        INSERT INTO order_items (
            "Id", "OrderId", "ProductId", "ProductName", "ProductImage", "Quantity", "PriceAtMoment", "Discount", "TotalPrice", "CompanyId",
            "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 1, 1, 'Кобзар', 'https://picsum.photos/seed/marketplace-book-1/200/300', 2, 349.00, 0.00, 698.00, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
         NOW(), NOW(), FALSE, NULL),
        (2, 2, 1, 'Кобзар', 'https://picsum.photos/seed/marketplace-book-1/200/300', 1, 349.00, 0.00, 349.00, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
         NOW(), NOW(), FALSE, NULL),
        (3, 2, 3, 'Тіні забутих предків', 'https://picsum.photos/seed/marketplace-book-3/200/300', 1, 279.00, 0.00, 279.00, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
         NOW(), NOW(), FALSE, NULL),
        (4, 3, 4, 'Intermezzo', 'https://picsum.photos/seed/marketplace-book-4/200/300', 1, 249.00, 0.00, 249.00, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
         NOW(), NOW(), FALSE, NULL),
        (5, 4, 1, 'Кобзар', 'https://picsum.photos/seed/marketplace-book-1/200/300', 2, 349.00, 0.00, 698.00, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
         NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'order_addresses') THEN
        INSERT INTO order_addresses (
            "Id", "OrderId", "Kind", "FirstName", "LastName", "Phone", "Street", "City", "State", "PostalCode", "Country",
            "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 1, 0, 'Admin', 'User', '+380000000001', 'Delivery street 1', 'Kyiv', 'Kyiv', '01001', 'UA', NOW(), NOW(), FALSE, NULL),
        (2, 1, 1, 'Admin', 'User', '+380000000001', 'Billing street 2', 'Kyiv', 'Kyiv', '01002', 'UA', NOW(), NOW(), FALSE, NULL),
        (3, 2, 0, 'Buyer', 'One', '+380000000003', 'Buyer delivery 5', 'Kyiv', 'Kyiv', '01005', 'UA', NOW(), NOW(), FALSE, NULL),
        (4, 2, 1, 'Buyer', 'One', '+380000000003', 'Buyer billing 5', 'Kyiv', 'Kyiv', '01005', 'UA', NOW(), NOW(), FALSE, NULL),
        (5, 3, 0, 'Buyer', 'One', '+380000000003', 'Lviv delivery 12', 'Lviv', 'Lviv', '79001', 'UA', NOW(), NOW(), FALSE, NULL),
        (6, 3, 1, 'Buyer', 'One', '+380000000003', 'Lviv billing 12', 'Lviv', 'Lviv', '79001', 'UA', NOW(), NOW(), FALSE, NULL),
        (7, 4, 0, 'Buyer', 'One', '+380000000003', 'Kyiv delivery 8', 'Kyiv', 'Kyiv', '01008', 'UA', NOW(), NOW(), FALSE, NULL),
        (8, 4, 1, 'Buyer', 'One', '+380000000003', 'Kyiv billing 8', 'Kyiv', 'Kyiv', '01008', 'UA', NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'order_status_history') THEN
        INSERT INTO order_status_history (
            "Id", "OrderId", "OldStatus", "NewStatus", "Comment", "ChangedByUserId", "Source", "CorrelationId",
            "ChangedAt", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 1, 0, 1, 'Payment captured', '11111111-1111-1111-1111-111111111111', 'seed', 'seed-ord-1-paid', NOW() - INTERVAL '5 days', NOW(), NOW(), FALSE, NULL),
        (2, 1, 1, 2, 'Warehouse picked', '99999999-9999-9999-9999-999999999999', 'seed', 'seed-ord-1-processing', NOW() - INTERVAL '4 days', NOW(), NOW(), FALSE, NULL),
        (3, 2, 0, 1, 'Paid via LiqPay', '33333333-3333-3333-3333-333333333333', 'seed', 'seed-ord-2-paid', NOW() - INTERVAL '3 days', NOW(), NOW(), FALSE, NULL),
        (4, 2, 1, 2, 'Packing', '99999999-9999-9999-9999-999999999999', 'seed', 'seed-ord-2-processing', NOW() - INTERVAL '2 days', NOW(), NOW(), FALSE, NULL),
        (5, 2, 2, 3, 'Handed to carrier', '99999999-9999-9999-9999-999999999999', 'seed', 'seed-ord-2-shipped', NOW() - INTERVAL '1 day', NOW(), NOW(), FALSE, NULL),
        (6, 3, 0, 1, 'Paid', '33333333-3333-3333-3333-333333333333', 'seed', 'seed-ord-3-paid', NOW() - INTERVAL '10 days', NOW(), NOW(), FALSE, NULL),
        (7, 3, 1, 2, 'Processing', '33333333-3333-3333-3333-333333333333', 'seed', 'seed-ord-3-processing', NOW() - INTERVAL '8 days', NOW(), NOW(), FALSE, NULL),
        (8, 3, 2, 3, 'Shipped', '99999999-9999-9999-9999-999999999999', 'seed', 'seed-ord-3-shipped', NOW() - INTERVAL '7 days', NOW(), NOW(), FALSE, NULL),
        (9, 3, 3, 4, 'Delivered', '99999999-9999-9999-9999-999999999999', 'seed', 'seed-ord-3-delivered', NOW() - INTERVAL '2 days', NOW(), NOW(), FALSE, NULL),
        (10, 4, 0, 1, 'Paid via LiqPay', '33333333-3333-3333-3333-333333333333', 'seed', 'seed-ord-4-paid', NOW() - INTERVAL '6 hours', NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'payments') THEN
        INSERT INTO payments (
            "Id", "OrderId", "PaymentMethod", "Amount", "Currency", "TransactionId", "Status", "ProviderResponseRaw", "ProcessedAt",
            "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 1, 2, 798.00, 'UAH', 'liqpay-seed-transaction-001', 1, '{"provider":"liqpay","seed":true,"status":"success"}', NOW() - INTERVAL '5 days', NOW(), NOW(), FALSE, NULL),
        (2, 2, 2, 628.00, 'UAH', 'liqpay-seed-transaction-002', 1, '{"provider":"liqpay","seed":true,"status":"success"}', NOW() - INTERVAL '3 days', NOW(), NOW(), FALSE, NULL),
        (3, 3, 2, 349.00, 'UAH', 'liqpay-seed-transaction-003', 1, '{"provider":"liqpay","seed":true,"status":"success"}', NOW() - INTERVAL '10 days', NOW(), NOW(), FALSE, NULL),
        (4, 4, 2, 698.00, 'UAH', 'liqpay-seed-transaction-004', 1, '{"provider":"liqpay","seed":true,"status":"success"}', NOW() - INTERVAL '6 hours', NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'refunds') THEN
        INSERT INTO refunds (
            "Id", "PaymentId", "OrderId", "Amount", "Reason", "Status", "ProcessedByUserId", "ProcessedAt", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 1, 1, 50.00, 'Seed partial refund', 1, '11111111-1111-1111-1111-111111111111', NOW(), NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'order_fulfillment_allocations') THEN
        INSERT INTO order_fulfillment_allocations (
            "Id", "OrderId", "OrderItemId", "CompanyId", "WarehouseId", "ProductId", "Quantity", "ReservationId", "Status",
            "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 4, 5, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 1, 1, 2, 2, NOW(), NOW(), FALSE, NULL),
        (2, 4, 5, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 2, 1, 1, 3, 2, NOW(), NOW(), FALSE, NULL),
        (3, 2, 2, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 1, 1, NULL, 3, NOW() - INTERVAL '1 day', NOW(), FALSE, NULL),
        (4, 2, 3, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 2, 3, 1, NULL, 3, NOW() - INTERVAL '1 day', NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'shipments') THEN
        INSERT INTO shipments (
            "Id", "OrderId", "CustomerId", "ShipmentNumber", "ShippingMethodId", "WarehouseId", "CarrierCode", "Status",
            "TrackingNumber", "LastSyncedAtUtc", "RawPayload", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 2, '33333333-3333-3333-3333-333333333333', 1, 1, 1, 0, 2,
         'NP-SEED-0002-A', NOW() - INTERVAL '1 day', '{"seed":true,"warehouse":"MAIN-KYIV"}', NOW() - INTERVAL '1 day', NOW(), FALSE, NULL),
        (2, 2, '33333333-3333-3333-3333-333333333333', 2, 1, 2, 0, 2,
         'NP-SEED-0002-B', NOW() - INTERVAL '1 day', '{"seed":true,"warehouse":"SEC-LVIV"}', NOW() - INTERVAL '1 day', NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'shipment_items') THEN
        INSERT INTO shipment_items ("Id", "ShipmentId", "OrderItemId", "Quantity", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt")
        VALUES
        (1, 1, 2, 1, NOW() - INTERVAL '1 day', NOW(), FALSE, NULL),
        (2, 2, 3, 1, NOW() - INTERVAL '1 day', NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'shipping_events') THEN
        INSERT INTO shipping_events (
            "Id", "CarrierCode", "EventKey", "PayloadHash", "RawPayload", "ReceivedAtUtc", "ShipmentId", "OrderId",
            "TrackingNumber", "DeliveryStatus", "OccurredAtUtc", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 0, 'seed-np-0002-a-shipped', 'hash-seed-np-0002-a', '{"status":"in_transit","seed":true}', NOW() - INTERVAL '1 day',
         1, 2, 'NP-SEED-0002-A', 2, NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day', NOW(), FALSE, NULL),
        (2, 0, 'seed-np-0002-b-shipped', 'hash-seed-np-0002-b', '{"status":"in_transit","seed":true}', NOW() - INTERVAL '1 day',
         2, 2, 'NP-SEED-0002-B', 2, NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day', NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'order_financials') THEN
        INSERT INTO order_financials (
            "Id", "OrderId", "PaymentId", "CompanyId", "Currency", "MerchandiseSubtotal", "DiscountAmount", "MerchandiseBase",
            "CommissionPercent", "PlatformFee", "SellerMerchandiseNet", "ShippingAmount", "SellerPayoutEligible",
            "PostedAtUtc", "CreatedAt", "UpdatedAt"
        ) VALUES
        (1, 2, 2, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'UAH', 628.00, 0.00, 628.00, 7.5000, 47.10, 580.90, 0.00, 580.90,
         NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days', NOW()),
        (2, 3, 3, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'UAH', 249.00, 0.00, 249.00, 6.2500, 15.56, 233.44, 100.00, 333.44,
         NOW() - INTERVAL '10 days', NOW() - INTERVAL '10 days', NOW());
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'settlement_batches') THEN
        INSERT INTO settlement_batches (
            "Id", "CompanyId", "PeriodStartUtc", "PeriodEndUtc", "Status", "TotalAmount", "Currency",
            "ClosedAtUtc", "PaidAtUtc", "CreatedAt", "UpdatedAt"
        ) VALUES
        (1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', NOW() - INTERVAL '14 days', NOW() - INTERVAL '7 days', 2, 580.90, 'UAH',
         NOW() - INTERVAL '7 days', NULL, NOW() - INTERVAL '14 days', NOW()),
        (2, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', NOW() - INTERVAL '21 days', NOW() - INTERVAL '14 days', 4, 333.44, 'UAH',
         NOW() - INTERVAL '14 days', NOW() - INTERVAL '13 days', NOW() - INTERVAL '21 days', NOW()),
        (3, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', NOW() - INTERVAL '7 days', NOW() - INTERVAL '1 day', 5, 0.00, 'UAH',
         NOW() - INTERVAL '1 day', NULL, NOW() - INTERVAL '7 days', NOW());
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'seller_payouts') THEN
        INSERT INTO seller_payouts (
            "Id", "CompanyId", "SettlementBatchId", "Status", "Amount", "Currency", "ProviderReference",
            "Iban", "RecipientName", "InitiatedAtUtc", "CompletedAtUtc", "FailureReason", "CreatedAt", "UpdatedAt"
        ) VALUES
        (1, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 2, 3, 333.44, 'UAH', 'MANUAL-SEED-001',
         NULL, 'Література Плюс LLC', NOW() - INTERVAL '14 days', NOW() - INTERVAL '13 days', NULL, NOW() - INTERVAL '14 days', NOW());
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'seller_ledger_entries') THEN
        INSERT INTO seller_ledger_entries (
            "Id", "CompanyId", "OrderId", "OrderFinancialsId", "SettlementBatchId", "SellerPayoutId",
            "EntryType", "Status", "Amount", "Currency", "Description", "AvailableAtUtc", "SettledAtUtc", "CreatedAt", "UpdatedAt"
        ) VALUES
        (1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 2, 1, 1, NULL, 1, 2, 580.90, 'UAH', 'Sale for order ORD-SEED-0002',
         NOW() - INTERVAL '1 day', NULL, NOW() - INTERVAL '3 days', NOW()),
        (2, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 2, 1, 1, NULL, 2, 2, 47.10, 'UAH', 'Platform fee for order ORD-SEED-0002',
         NOW() - INTERVAL '1 day', NULL, NOW() - INTERVAL '3 days', NOW()),
        (3, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 3, 2, 2, 1, 1, 3, 333.44, 'UAH', 'Sale for order ORD-SEED-0003',
         NOW() - INTERVAL '2 days', NOW() - INTERVAL '13 days', NOW() - INTERVAL '10 days', NOW()),
        (4, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 3, 2, 2, 1, 2, 3, 15.56, 'UAH', 'Platform fee for order ORD-SEED-0003',
         NOW() - INTERVAL '2 days', NOW() - INTERVAL '13 days', NOW() - INTERVAL '10 days', NOW()),
        (5, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, NULL, NULL, NULL, 3, 2, -50.00, 'UAH', 'Seed partial refund',
         NOW(), NULL, NOW(), NOW());
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'coupons') THEN
        INSERT INTO coupons (
            "Id", "Code", "Description", "DiscountAmount", "DiscountType", "MinOrderAmount", "UsageLimit", "UsageCount",
            "UserUsageLimit", "ExpiresAtUtc", "StartsAtUtc", "ApplicableCategoriesRaw", "ApplicableProductsRaw",
            "ApplicableCompaniesRaw", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 'SEED10', 'Seed 10% off Книжковий світ', 10.00, 0, NULL, 100, 0, 1,
         NOW() + INTERVAL '365 days', NOW() - INTERVAL '30 days', NULL, NULL,
         '["aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"]'::jsonb, TRUE, NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'return_requests') THEN
        INSERT INTO return_requests (
            "Id", "OrderId", "CustomerId", "CompanyId", "Status", "ReasonCode", "Comment",
            "ApprovedByUserId", "RejectedReason", "ReceivedAtUtc", "RefundId", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 3, '33333333-3333-3333-3333-333333333333', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 0, 2,
         'Received wrong book edition', NULL, NULL, NULL, NULL, NOW() - INTERVAL '1 day', NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'return_line_items') THEN
        INSERT INTO return_line_items ("Id", "ReturnRequestId", "OrderItemId", "Quantity", "Reason", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt")
        VALUES (1, 1, 4, 1, 'WrongItem', NOW() - INTERVAL '1 day', NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'chats') THEN
        INSERT INTO chats (
            "Id", "Type", "Status", "InitiatorUserId", "OrderId", "ProductId", "LastMessageText", "LastMessageSenderId",
            "LastMessageCreatedAt", "IsActive", "Meta", "ParticipantsSnapshot", "RawPayload", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        ('c1000001-0000-4000-8000-000000000001', 2, 0, '33333333-3333-3333-3333-333333333333', 2, NULL,
         'When will the second book shipment arrive?', '33333333-3333-3333-3333-333333333333', NOW() - INTERVAL '20 hours',
         TRUE, '{"seed":true}', NULL, NULL, NOW() - INTERVAL '1 day', NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'chat_participants') THEN
        INSERT INTO chat_participants (
            "ChatId", "UserId", "Role", "CompanyId", "JoinedAt", "LeftAt", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        ('c1000001-0000-4000-8000-000000000001', '33333333-3333-3333-3333-333333333333', 0, NULL, NOW() - INTERVAL '1 day', NULL, NOW() - INTERVAL '1 day', NOW(), FALSE, NULL),
        ('c1000001-0000-4000-8000-000000000001', '22222222-2222-2222-2222-222222222222', 1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', NOW() - INTERVAL '1 day', NULL, NOW() - INTERVAL '1 day', NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'chat_messages') THEN
        INSERT INTO chat_messages (
            "Id", "ChatId", "SenderId", "Text", "Attachments", "Status", "ReadAt", "DeletedBy", "ReplyToMessageId",
            "RawPayload", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 'c1000001-0000-4000-8000-000000000001', '33333333-3333-3333-3333-333333333333',
         'Hi, I see two tracking numbers for my order ORD-SEED-0002.', '[]', 0, NULL, '{}', NULL, NULL,
         NOW() - INTERVAL '22 hours', NOW(), FALSE, NULL),
        (2, 'c1000001-0000-4000-8000-000000000001', '33333333-3333-3333-3333-333333333333',
         'When will the second book shipment arrive?', '[]', 0, NULL, '{}', NULL, NULL,
         NOW() - INTERVAL '20 hours', NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'chat_read_states') THEN
        INSERT INTO chat_read_states ("ChatId", "UserId", "LastReadMessageId", "UpdatedAt")
        VALUES
        ('c1000001-0000-4000-8000-000000000001', '22222222-2222-2222-2222-222222222222', 1, NOW() - INTERVAL '18 hours');
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'product_reviews') THEN
        INSERT INTO product_reviews (
            "Id", "ProductId", "UserId", "UserName", "UserAvatar", "Rating", "Title", "Comment", "ImagesRaw", "ProsRaw", "ConsRaw",
            "IsVerifiedPurchase", "OrderId", "HelpfulRaw", "Status", "ModeratedByUserId", "ModeratedAt", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 1, '11111111-1111-1111-1111-111111111111', 'Admin User', NULL, 5, 'Чудове видання', 'Класика української літератури',
         '[]'::jsonb, '["мова","якість друку"]'::jsonb, '[]'::jsonb, TRUE, 1, '{"up":3,"down":0}'::jsonb, 1, '44444444-4444-4444-4444-444444444444', NOW(), NOW(), NOW(), FALSE, NULL),
        (2, 4, '55555555-5555-5555-5555-555555555555', 'Plain User', NULL, 4, 'Гарна книга', 'Швидка доставка',
         '[]'::jsonb, '["цікавий сюжет"]'::jsonb, '["мала обкладинка"]'::jsonb, FALSE, NULL, '{"up":1,"down":0}'::jsonb, 1, '44444444-4444-4444-4444-444444444444', NOW(), NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'company_reviews') THEN
        INSERT INTO company_reviews (
            "Id", "CompanyId", "UserId", "UserName", "OrderId", "IsVerifiedPurchase", "RatingsRaw", "OverallRating", "Comment",
            "Status", "ModeratedByUserId", "ModeratedAt", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111', 'Admin User', 1, TRUE,
         '{"service":5,"shipping":4}'::jsonb, 4.50, 'Fast support and delivery', 1, '44444444-4444-4444-4444-444444444444', NOW(), NOW(), NOW(), FALSE, NULL),
        (2, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '55555555-5555-5555-5555-555555555555', 'Plain User', NULL, FALSE,
         '{"service":4,"quality":4}'::jsonb, 4.00, 'Nice assortment', 1, '44444444-4444-4444-4444-444444444444', NOW(), NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'review_replies') THEN
        INSERT INTO review_replies (
            "Id", "ProductReviewId", "CompanyReviewId", "CompanyId", "AuthorUserId", "Body", "IsEdited",
            "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 1, NULL, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111', 'Дякуємо за відгук про книгу!', FALSE,
         NOW(), NOW(), FALSE, NULL),
        (2, NULL, 1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111', 'We appreciate your review and will keep improving.', FALSE,
         NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'notifications') THEN
        INSERT INTO notifications (
            "Id", "UserId", "Type", "Title", "Message", "Data", "ActionUrl", "IsRead", "ReadAt", "ExpiresAt", "RawPayload", "CorrelationId",
            "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, '33333333-3333-3333-3333-333333333333', 0, 'Замовлення відправлено',
         'Ваше замовлення ORD-SEED-0002 передано перевізнику.',
         '{"templateKey":"UserOrderStatus","jobCorrelationId":"b1000001-0000-4000-8000-000000000001","orderId":2,"status":"Shipped"}'::jsonb,
         '/me/orders/2', FALSE, NULL, NOW() + INTERVAL '90 days',
         '{"templateKey":"UserOrderStatus","orderId":2}'::jsonb,
         'b1000001-0000-4000-8000-000000000001', NOW() - INTERVAL '1 day', NOW(), FALSE, NULL),
        (2, '33333333-3333-3333-3333-333333333333', 0, 'Товар знову в наявності',
         'Лісова пісня знову доступна для замовлення.',
         '{"templateKey":"CartProductBackInStock","jobCorrelationId":"b1000002-0000-4000-8000-000000000002","productId":2,"slug":"seed-book-002"}'::jsonb,
         '/catalog/products/seed-book-002', TRUE, NOW() - INTERVAL '12 hours', NOW() + INTERVAL '90 days',
         '{"templateKey":"CartProductBackInStock","productId":2}'::jsonb,
         'b1000002-0000-4000-8000-000000000002', NOW() - INTERVAL '2 days', NOW(), FALSE, NULL),
        (3, '11111111-1111-1111-1111-111111111111', 0, 'Нове замовлення',
         'Надійшло замовлення ORD-SEED-0001 у Книжковий світ.',
         '{"templateKey":"AdminNewOrder","jobCorrelationId":"a1000001-0000-4000-8000-000000000001","orderId":1}'::jsonb,
         '/admin/orders/1', FALSE, NULL, NOW() + INTERVAL '90 days',
         '{"templateKey":"AdminNewOrder","orderId":1}'::jsonb,
         'a1000001-0000-4000-8000-000000000001', NOW() - INTERVAL '5 days', NOW(), FALSE, NULL),
        (4, '44444444-4444-4444-4444-444444444444', 3, 'Товар на модерації',
         'Книга на модерації очікує перевірки.',
         '{"templateKey":"AdminProductPendingReview","jobCorrelationId":"a1000002-0000-4000-8000-000000000002","productId":243}'::jsonb,
         '/admin/products/pending', FALSE, NULL, NOW() + INTERVAL '90 days',
         '{"templateKey":"AdminProductPendingReview","productId":243}'::jsonb,
         'a1000002-0000-4000-8000-000000000002', NOW() - INTERVAL '6 hours', NOW(), FALSE, NULL),
        (5, '22222222-2222-2222-2222-222222222222', 3, 'Товар схвалено',
         'Вашу книгу на модерації опубліковано.',
         '{"templateKey":"UserProductApproved","jobCorrelationId":"a1000003-0000-4000-8000-000000000003","productId":243}'::jsonb,
         '/companies/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/products/243', TRUE, NOW() - INTERVAL '1 hour', NOW() + INTERVAL '90 days',
         '{"templateKey":"UserProductApproved","productId":243}'::jsonb,
         'a1000003-0000-4000-8000-000000000003', NOW() - INTERVAL '3 hours', NOW(), FALSE, NULL),
        (6, '77777777-7777-7777-7777-777777777777', 3, 'Товар відхилено',
         'Відхилена книга не пройшла модерацію.',
         '{"templateKey":"UserProductRejected","jobCorrelationId":"a1000004-0000-4000-8000-000000000004","productId":244,"reason":"Обкладинка не відповідає опису книги."}'::jsonb,
         NULL, FALSE, NULL, NOW() + INTERVAL '90 days',
         '{"templateKey":"UserProductRejected","productId":244}'::jsonb,
         'a1000004-0000-4000-8000-000000000004', NOW() - INTERVAL '1 day', NOW(), FALSE, NULL),
        (7, '22222222-2222-2222-2222-222222222222', 0, 'Нове замовлення у вашій компанії',
         'Покупець оформив ORD-SEED-0002.',
         '{"templateKey":"CompanyNewOrder","jobCorrelationId":"c1000001-0000-4000-8000-000000000005","orderId":2,"companyId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"}'::jsonb,
         '/companies/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/orders/2', FALSE, NULL, NOW() + INTERVAL '90 days',
         '{"templateKey":"CompanyNewOrder","orderId":2}'::jsonb,
         'c1000001-0000-4000-8000-000000000005', NOW() - INTERVAL '3 days', NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'push_subscriptions') THEN
        INSERT INTO push_subscriptions ("Id", "UserId", "Endpoint", "P256dh", "Auth", "AudienceFlags", "UserAgent", "CreatedAtUtc", "LastUsedAtUtc")
        VALUES
        (1, '33333333-3333-3333-3333-333333333333',
         'https://fcm.googleapis.com/fcm/send/seed-buyer-endpoint-0001',
         'BNcRdreALRieniP3kFbOqLzc2v6Ip5z0eNc0iR12fHcKzv',
         'tBHIjz7k0DFYKBJ6FFeq2g',
         1, 'Mozilla/5.0 (Seed Browser)', NOW() - INTERVAL '7 days', NOW() - INTERVAL '1 day'),
        (2, '11111111-1111-1111-1111-111111111111',
         'https://fcm.googleapis.com/fcm/send/seed-admin-endpoint-0001',
         'BKxRdreALRieniP3kFbOqLzc2v6Ip5z0eNc0iR12fHcKzv',
         'x8Yz7k0DFYKBJ6FFeq2g',
         3, 'Mozilla/5.0 (Seed Admin)', NOW() - INTERVAL '14 days', NOW() - INTERVAL '2 hours'),
        (3, '44444444-4444-4444-4444-444444444444',
         'https://updates.push.services.mozilla.com/wpush/v2/seed-mod-endpoint',
         'BModeratorPushKeyExample0000000000000000001',
         'authModeratorSeed0001',
         2, 'Mozilla/5.0 (Seed Moderator)', NOW() - INTERVAL '3 days', NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'outbox_messages') THEN
        INSERT INTO outbox_messages (
            "Id", "AggregateType", "AggregateId", "EventType", "Payload", "OccurredAtUtc", "ProcessedAtUtc",
            "Attempts", "LastError", "NextAttemptAtUtc", "DeadLetteredAtUtc", "DeadLetterReason", "DeadLetterCategory"
        ) VALUES
        ('f0000001-0000-0000-0000-000000000001', 'Order', '2', 'OrderStatusChanged',
         '{"orderId":2,"oldStatus":2,"newStatus":3,"seed":true}', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day',
         1, NULL, NULL, NULL, NULL, NULL),
        ('f0000002-0000-0000-0000-000000000002', 'Inventory', '1', 'StockReceived',
         '{"productId":1,"warehouseId":1,"quantity":10,"seed":true}', NOW() - INTERVAL '30 minutes', NULL,
         0, NULL, NULL, NULL, NULL, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'inbox_messages') THEN
        INSERT INTO inbox_messages ("MessageId", "Consumer", "ProcessedAtUtc", "Metadata")
        VALUES
        ('f0000001-0000-0000-0000-000000000001', 'OutboxEventProcessor', NOW() - INTERVAL '1 day', '{"seed":true}');
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'http_idempotency_requests') THEN
        INSERT INTO http_idempotency_requests (
            "Scope", "IdempotencyKey", "RequestHash", "Status", "ResponseStatusCode", "ResponseBodyJson",
            "CreatedAtUtc", "ExpiresAtUtc", "CompletedAtUtc"
        ) VALUES
        ('POST:/me/cart/checkout', 'seed-checkout-key-001', 'hash-seed-checkout-001', 'Completed', 200,
         '{"orderIds":[2],"seed":true}', NOW() - INTERVAL '3 days', NOW() + INTERVAL '24 hours', NOW() - INTERVAL '3 days');
    END IF;
    END IF;
END $$;

-- 8) Align identity sequences after explicit Ids
DO $$
DECLARE seq_name text;
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'products') THEN
        seq_name := pg_get_serial_sequence('products', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM products), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'product_details') THEN
        seq_name := pg_get_serial_sequence('product_details', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM product_details), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'product_images') THEN
        seq_name := pg_get_serial_sequence('product_images', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM product_images), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'warehouses') THEN
        seq_name := pg_get_serial_sequence('warehouses', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM warehouses), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'warehouse_stocks') THEN
        seq_name := pg_get_serial_sequence('warehouse_stocks', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM warehouse_stocks), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'stock_movements') THEN
        seq_name := pg_get_serial_sequence('stock_movements', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM stock_movements), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'inventory_reservations') THEN
        seq_name := pg_get_serial_sequence('inventory_reservations', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM inventory_reservations), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'company_legal_profiles') THEN
        seq_name := pg_get_serial_sequence('company_legal_profiles', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM company_legal_profiles), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'company_contracts') THEN
        seq_name := pg_get_serial_sequence('company_contracts', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM company_contracts), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'company_commission_rates') THEN
        seq_name := pg_get_serial_sequence('company_commission_rates', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM company_commission_rates), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'carts') THEN
        seq_name := pg_get_serial_sequence('carts', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM carts), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'cart_items') THEN
        seq_name := pg_get_serial_sequence('cart_items', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM cart_items), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'favorites') THEN
        seq_name := pg_get_serial_sequence('favorites', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM favorites), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'orders') THEN
        seq_name := pg_get_serial_sequence('orders', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM orders), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'order_items') THEN
        seq_name := pg_get_serial_sequence('order_items', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM order_items), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'order_addresses') THEN
        seq_name := pg_get_serial_sequence('order_addresses', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM order_addresses), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'payments') THEN
        seq_name := pg_get_serial_sequence('payments', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM payments), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'refunds') THEN
        seq_name := pg_get_serial_sequence('refunds', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM refunds), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'product_reviews') THEN
        seq_name := pg_get_serial_sequence('product_reviews', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM product_reviews), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'company_reviews') THEN
        seq_name := pg_get_serial_sequence('company_reviews', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM company_reviews), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'review_replies') THEN
        seq_name := pg_get_serial_sequence('review_replies', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM review_replies), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'order_status_history') THEN
        seq_name := pg_get_serial_sequence('order_status_history', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM order_status_history), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'notifications') THEN
        seq_name := pg_get_serial_sequence('notifications', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM notifications), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'push_subscriptions') THEN
        seq_name := pg_get_serial_sequence('push_subscriptions', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM push_subscriptions), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'cart_stock_watches') THEN
        seq_name := pg_get_serial_sequence('cart_stock_watches', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM cart_stock_watches), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'order_fulfillment_allocations') THEN
        seq_name := pg_get_serial_sequence('order_fulfillment_allocations', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM order_fulfillment_allocations), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'shipments') THEN
        seq_name := pg_get_serial_sequence('shipments', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM shipments), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'shipment_items') THEN
        seq_name := pg_get_serial_sequence('shipment_items', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM shipment_items), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'shipping_events') THEN
        seq_name := pg_get_serial_sequence('shipping_events', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM shipping_events), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'order_financials') THEN
        seq_name := pg_get_serial_sequence('order_financials', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM order_financials), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'settlement_batches') THEN
        seq_name := pg_get_serial_sequence('settlement_batches', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM settlement_batches), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'seller_payouts') THEN
        seq_name := pg_get_serial_sequence('seller_payouts', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM seller_payouts), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'seller_ledger_entries') THEN
        seq_name := pg_get_serial_sequence('seller_ledger_entries', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM seller_ledger_entries), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'coupons') THEN
        seq_name := pg_get_serial_sequence('coupons', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM coupons), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'return_requests') THEN
        seq_name := pg_get_serial_sequence('return_requests', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM return_requests), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'return_line_items') THEN
        seq_name := pg_get_serial_sequence('return_line_items', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM return_line_items), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'chat_messages') THEN
        seq_name := pg_get_serial_sequence('chat_messages', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM chat_messages), true);
        END IF;
    END IF;
END $$;

COMMIT;
